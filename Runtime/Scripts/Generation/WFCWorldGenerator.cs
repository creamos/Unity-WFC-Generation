using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WFCWorldGenerator : MonoBehaviour
{
#if NAUGHTY_ATTR
    [NaughtyAttributes.ValidateInput("BindPrototypeLut", "please validate the PrototypeLUT !")]
#endif
    [SerializeField] 
    private PrototypeLUT _prototypeLut;

    [Header("Generation Settings")] [SerializeField]
    [Tooltip("Random number seed used by the WFC generator.")]
    private int seed = 0;

    private System.Random randomGenerator;

    [Tooltip("Width of the 3D cell grid in which the generation is performed.")]
    [SerializeField, Min(1)] private int worldSize = 100;
    [Tooltip("Height of the 3D cell grid in which the generation is performed.")]
    [SerializeField, Min(1)] private int worldHeight = 4;

    private int iterator = 0, propagIteration = 0;

    private enum EntropyMode
    {
        Simple,
        Default
    };

    [Tooltip("If true, entropy of the cells and module selection is affected by their respective weights.")]
    [SerializeField] private bool useWeight = true;
    
#if NAUGHTY_ATTR
    [NaughtyAttributes.ShowIf("useWeight")]
#endif
    [Tooltip("Affects how entropy is calculated:" +
             "\n- Simple : only proportional to the probable modules." +
             "\n- Default : also affected by their respective weights.")]
    [SerializeField] private EntropyMode _entropyCalculation;

    [Tooltip("Forces to first collapse a tile at the lowest possible height.")]
    [SerializeField] private bool _firstStepAtGroundLevel = true;

    [Tooltip("Transform under which modules are generated.")]
    [SerializeField] private Transform _spawnedModulesContainer;

    private Transform SpawnedModulesContainer =>
        _spawnedModulesContainer != null
            ? _spawnedModulesContainer
            : transform;

    [Header("Simulation Settings")] [SerializeField]
    [Tooltip("In playmode, should the generation happen in one frame or over time.")]
    private bool generateAsynchronously = true;

#if NAUGHTY_ATTR
    [NaughtyAttributes.ShowIf("generateAsynchronously")]
#endif
    [Tooltip("If set, generation happens step by step, waiting for the user inputs.")]
    [SerializeField] private bool generateManually = false;

    [Tooltip("Duration between steps when running asynchronously.")]
    [SerializeField] [Range(0f, 1f)] private float generationStep = .1f;

    private List<int>[,,] _grid;
    private int _nbTiles;

    private GenerationTrackList _generationHistory;
    [Tooltip("Previous steps count memorized by the generator. Used to perform backtracking.")]
    [SerializeField, Min(100)] private uint _backTrackBufferSize = 100;

#if NAUGHTY_ATTR
    [NaughtyAttributes.ReadOnly]
#endif
    [Tooltip("Number of previous steps currently memorized.")]
    [SerializeField] private int generationHistoryStep = 0;

    [Header("Error-related stuff")] [SerializeField]
    [Tooltip("Number of allowed unsuccessful iterations before breaking (to prevent infinite-loop)..")]
    int MAX_ITERATION = 100000;


#if UNITY_EDITOR
    
#if NAUGHTY_ATTR
    [NaughtyAttributes.BoxGroup("Debug")]
#endif
    [SerializeField] bool _drawDebug = false;

#if NAUGHTY_ATTR
    [NaughtyAttributes.BoxGroup("Debug")] [NaughtyAttributes.ShowIf("_drawDebug")]
#endif
    [SerializeField] 
    bool _drawBoundaries = true, _drawEntropies = false;

    private bool BindPrototypeLut(PrototypeLUT value)
    {
        if (value == null) _prototypeLut = PrototypeLUT.Editor_GetInstance();
        return true;
    }
#endif

    private List<int> GetCell(Vector3Int cellPos) => _grid[cellPos.x, cellPos.y, cellPos.z];
    private List<int> GetCell(int x, int y, int z) => _grid[x, y, z];

    void Start()
    {
        ClearModules();
        if (generateAsynchronously)
            StartCoroutine(WFC_Crt());
        else
            WFC();
    }


#if NAUGHTY_ATTR
    [NaughtyAttributes.Button("Clear", NaughtyAttributes.EButtonEnableMode.Editor)]
#endif
    void ClearModules()
    {
        for (int i = SpawnedModulesContainer.childCount - 1; i >= 0; --i)
        {
            if (Application.isEditor && !Application.isPlaying)
                DestroyImmediate(SpawnedModulesContainer.GetChild(i).gameObject);
            else
                Destroy(SpawnedModulesContainer.GetChild(i).gameObject);
        }
    }
    
#if NAUGHTY_ATTR
    [NaughtyAttributes.Button("Run", NaughtyAttributes.EButtonEnableMode.Editor)]
#endif
    void Generate()
    {
        ClearModules();
        WFC();
    }


    public void WFC()
    {
        iterator = 0;
        Initialize();
        generationHistoryStep = 1;

        // First step at ground level
        IterationResult genStepContext = PerformGenerationStep(_firstStepAtGroundLevel);
        generationHistoryStep = ProcessIterationResult(genStepContext);

        while (!IsMapGenerated())
        {
            iterator++;
            genStepContext = PerformGenerationStep();
            generationHistoryStep = ProcessIterationResult(genStepContext);
        }
    }

    public IEnumerator WFC_Crt()
    {
        iterator = 0;
        Initialize();
        generationHistoryStep = 1;

        // First step at ground level
        IterationResult genStepContext = PerformGenerationStep(_firstStepAtGroundLevel);
        generationHistoryStep = ProcessIterationResult(genStepContext);

        if (generateManually) yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        else yield return new WaitForSeconds(generationStep);

        while (!IsMapGenerated())
        {
            iterator++;
            genStepContext = PerformGenerationStep();
            generationHistoryStep = ProcessIterationResult(genStepContext);

            if (generateManually) yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
            else yield return new WaitForSeconds(generationStep);
        }
    }

    private void Initialize()
    {
        if (seed != -1)
        {
            randomGenerator = new System.Random(seed);
            //Random.InitState(seed);
        }
        else
        {
            int generatedSeed = (int)DateTime.Now.Ticks;
            randomGenerator = new System.Random(generatedSeed);
            //Random.InitState(generatedSeed);
            Debug.Log("Generated seed : " + generatedSeed);
        }

        _generationHistory = new GenerationTrackList(_backTrackBufferSize);

        _nbTiles = worldSize * worldHeight * worldSize;
        _grid = new List<int>[worldSize, worldHeight, worldSize];
        for (int z = 0; z < worldSize; z++)
        for (int y = 0; y < worldHeight; y++)
        for (int x = 0; x < worldSize; x++)
            _grid[x, y, z] = Enumerable.Range(0, _prototypeLut.Count)
                .Where(protoID =>
                {
                    var flag = _prototypeLut.GetPrototype(protoID).flag;
                    bool groundCondition = y == 0 
                        ? (flag & ModuleFlag.Grounded) == ModuleFlag.Grounded
                        : (flag & ModuleFlag.Airborne) == ModuleFlag.Airborne;
                    bool roofCondition = y != worldHeight - 1 || (flag & (ModuleFlag.Roof)) == ModuleFlag.Roof;

                    bool edgeCondition = 
                        ((x != 0 && x != worldSize - 1)
                        && (z != 0 && z != worldSize - 1))
                        || (flag & ModuleFlag.Inside) == 0;

                    return groundCondition && roofCondition;// && edgeCondition;
                }).ToList();
    }

    private IterationResult PerformGenerationStep(bool groundOnly = false)
    {
        List<CellEntropy> modifiedEntropies = new List<CellEntropy>();

        #region Select & Collapse Cell

        //get a cell with lowest entropy
        CollapseStatus collapseStatus =
            FindLowestEntropy(groundOnly, out Vector3Int chosenCell, out Vector3Int[] allCellsFound);

        // skip propagation & return if no cell could be picked
        if (collapseStatus == CollapseStatus.nullEntropy)
        {
            return new IterationResult
            {
                result = collapseStatus
            };
        }
        
        modifiedEntropies.Add(new CellEntropy { pos = chosenCell, entropy = GetCell(chosenCell) });

        //lock entropy (chose and spawn a random available tile at the cell position)
        int chosenState = LockEntropy(chosenCell);
        if (generationHistoryStep == 0) _generationHistory.UpdateLastEntry(chosenCell);

        #endregion

        #region Propagate Collapse

        //update constraints upon neighbours
        PropagationResult propagationContext = GeneratePropagation(chosenCell);
        modifiedEntropies.AddRange(propagationContext.modifiedEntropies
            .Where(newlyModifiedEntropy => modifiedEntropies.All(entropy => entropy.pos != newlyModifiedEntropy.pos)));

        // skip module generation & return if the propagation encountered any issue
        if (propagationContext.result != CollapseStatus.performed)
        {
            return new IterationResult
            {
                result = propagationContext.result
            };
        }

        #endregion

        #region Instantiate Modules if Valid

        // Instantiate collapsed cells & add generated instances to the list entries
        GameObject collapsedCellInstance = GenerateModuleInstance(chosenCell, chosenState);

        propagationContext.collapsedCells = propagationContext.collapsedCells.Select(collapseInfo =>
        {
            GameObject instance = GenerateModuleInstance(collapseInfo.position, collapseInfo.prototypeID);
            return new CollapseInfo(collapseInfo.position, collapseInfo.prototypeID, instance);
        }).ToArray();

        #endregion

        #region Process Results

        CollapseInfo collapsedCell = new CollapseInfo
        {
            position = chosenCell,
            prototypeID = chosenState,
            instance = collapsedCellInstance
        };

        #endregion


        return new IterationResult
        {
            result = collapseStatus,
            modifiedCellEntropies = modifiedEntropies.ToArray(),
            collapsedCell = collapsedCell,
            propagationCollapsedCells = propagationContext.collapsedCells,
            cellsWithLowestEntropy = allCellsFound,
            generatedInstances = propagationContext.collapsedCells
                .Select(collapsedInfo => collapsedInfo.instance)
                .Append(collapsedCellInstance)
                .ToArray(),
        };
    }

    private CollapseStatus FindLowestEntropy(bool groundOnly, out Vector3Int chosenCell, out Vector3Int[] allCellsFound)
    {
        chosenCell = default;

        if (generationHistoryStep == 0)
        {
            var entryData = _generationHistory.Peek();
            allCellsFound = entryData.remainingCells.ToArray();
        }
        else
        {
            List<Vector3Int> cellsWithLowestEntropy = new List<Vector3Int>();

            float lowestEntropy = float.PositiveInfinity;

            for (int z = 0; z < worldSize; z++)
            for (int y = 0; y < worldHeight; y++)
            for (int x = 0; x < worldSize; x++)
            {
                float entropy = GetEntropy(new Vector3Int(x, y, z));
                if (entropy < lowestEntropy && entropy > 0)
                {
                    cellsWithLowestEntropy.Clear();
                    lowestEntropy = entropy;
                    cellsWithLowestEntropy.Add(new Vector3Int(x, y, z));
                }
                else if (entropy == lowestEntropy) cellsWithLowestEntropy.Add(new Vector3Int(x, y, z));
            }

            allCellsFound = cellsWithLowestEntropy
                .Where(cell => !groundOnly || cell.y == 0) // only choose cell at ground level if necessary
                .ToArray();
        }

        // Interrupt Cell picking if constraints can't be solved
        if (allCellsFound.Length == 0)
        {
            return CollapseStatus.nullEntropy;
        }

        // Choose a cell between those available
        else if (allCellsFound.Length == 1)
        {
            chosenCell = allCellsFound[0];

            return CollapseStatus.singleCollapsed;
        }
        else
        {
            chosenCell = allCellsFound[randomGenerator.Next(allCellsFound.Length)];

            return CollapseStatus.randomCollapse;
        }
    }

    /// <summary>
    /// lock entropy (chose and spawn a random available tile at the cell position)
    /// </summary>
    /// <param name="currentCell"></param>
    /// <returns></returns>
    private int LockEntropy(Vector3Int currentCell)
    {
        int chosenState = GetCell(currentCell) switch
        {
            var entropy when entropy.Count == 0 => -1,
            var entropy when entropy.Count == 1 => entropy[0],
            _ => useWeight ? ChooseTileByWeight(currentCell) : ChooseTileRandom(currentCell)
        };

        if (chosenState == -1) return chosenState;

        _grid[currentCell.x, currentCell.y, currentCell.z] = new List<int> { chosenState };
        _nbTiles--;
        
        return chosenState;
    }

    /// <summary>
    /// return the ID of the chosen tile by Weight, using Tab Rand.
    /// </summary>
    /// <param name="currentCell"></param>
    /// <returns></returns>
    private int ChooseTileByWeight(Vector3Int currentCell)
    {
        float sumWeight;
        List<int> cellEntropy;

        int choice = 0;
        
        if (generationHistoryStep == 0)
        {
            if (_generationHistory.Count == 0) 
                Debug.LogError("Can't peek empty history");
            var entryData = _generationHistory.Peek();
            var remainingCellsHistoryIDs = entryData.remainingCells
                .Select((cell, id) => (pos: cell, id: id))
                .Where(cell => cell.pos == currentCell);
            int cellID = 0;
            if (remainingCellsHistoryIDs.Count() > 0)
                cellID = remainingCellsHistoryIDs.First().id;
            else 
                Debug.LogError("ERRRROR");
            cellEntropy = entryData.remainingEntropies[cellID];
        }
        else
        {
            choice = 1;
            cellEntropy = GetCell(currentCell);
        }

        sumWeight = cellEntropy.Sum(availableTile => _prototypeLut.GetPrototype(availableTile).weight);
        float randomWeight = (float)randomGenerator.NextDouble() * sumWeight;
        int chosenID = 0;

        if (cellEntropy.Count == 0)
        {
            string log = $"Error (iter {iterator}/{propagIteration}) at {currentCell.ToString()}";
            for (int i = -1; i < 2; i++)
            for (int j = -1; j < 2; j++)
            for (int k = -1; k < 2; k++)
            {
                if (Mathf.Abs(i) + Mathf.Abs(j) + Mathf.Abs(k) == 1)
                {
                    Vector3Int pos = currentCell + new Vector3Int(i, j, k);

                    if (pos.x < 0 || pos.x >= worldSize
                        || pos.y < 0 || pos.y >= worldHeight
                        || pos.z < 0 || pos.z >= worldSize) 
                        continue;

                    var neighbour = GetCell(pos);
                    log += $"\n({i},{j},{k}): {neighbour.Count}";
                }
            }

            Debug.LogWarning(log);
        }
        else
        {
            float weight = _prototypeLut.GetPrototype(cellEntropy[chosenID]).weight;
            while (weight < randomWeight)
            {
                randomWeight -= weight;
                chosenID++;
                weight = _prototypeLut.GetPrototype(cellEntropy[chosenID]).weight;
            }
        }

        return cellEntropy[chosenID];
    }

    private int ChooseTileRandom(Vector3Int currentCell)
    {
        List<int> cellEntropy;
        if (generationHistoryStep == 0)
        {
            var entryData = _generationHistory.Peek();
            int cellID = entryData.remainingCells.Select((cell, id) => (pos: cell, id: id))
                .Where(cell => cell.pos == currentCell).First().id;
            cellEntropy = entryData.remainingEntropies[cellID];
        }
        else
        {
            cellEntropy = GetCell(currentCell);
        }

        int chosenID = randomGenerator.Next(0, cellEntropy.Count);

        return cellEntropy[chosenID];
    }


    private PropagationResult GeneratePropagation(Vector3Int currentCell)
    {
        Stack<Vector3Int> propagationStack = new();
        List<CollapseInfo> collapsedCells = new();
        List<CellEntropy> modifiedEntropies = new();

        propagIteration = 0;
        
        propagationStack.Push(currentCell);

        bool validPropagation = true;
        int iteration = 0;

        while (validPropagation && propagationStack.Count > 0 && iteration++ < MAX_ITERATION)
        {
            propagIteration++;
            validPropagation = LocalPropagation(propagationStack.Pop(), propagationStack, collapsedCells,
                ref modifiedEntropies);
        }

        // Early return if an infinite loop was detected
        if (iteration >= MAX_ITERATION)
        {
            Debug.LogError("infinite loop detected in GeneratePropagation method.");
            return new PropagationResult { result = CollapseStatus.infiniteLoop };
        }

        // Empty collapsing list if the propagation encounter any issue
        if (!validPropagation)
        {
            collapsedCells = null;
        }


        propagIteration = 0;

        // Generate the propagation result data structure
        return new PropagationResult
        {
            result = validPropagation ? CollapseStatus.performed : CollapseStatus.nullEntropy,
            collapsedCells = collapsedCells?.ToArray(),
            modifiedEntropies = modifiedEntropies.ToArray()
        };
    }

    private bool LocalPropagation(Vector3Int currentCell, Stack<Vector3Int> propagationStack,
        List<CollapseInfo> collapsedCells, ref List<CellEntropy> modifiedEntropies)
    {
        List<Vector3Int> adjacentsCells = GetAdjacentCells(currentCell);
        Prototype[] cellPrototypes = GetCell(currentCell).Select(prototypeID => _prototypeLut[prototypeID]).ToArray();

        foreach (var direction in adjacentsCells.Select(neighbour => neighbour - currentCell))
        {
            //define all the constraints for our cell in a given direction
            List<int> constraints = new List<int>();
            foreach (Prototype cellPrototype in cellPrototypes)
                constraints.AddRange(cellPrototype.GetConstraints(direction));
            constraints = constraints.Distinct().ToList();

            // buffer the current entropy of the cell in case it is modified by the constraints
            var cellEntropy = new CellEntropy
            {
                pos = currentCell + direction,
                entropy = GetCell(currentCell + direction)
            };

            // reduce entropy of the neighbour with the given constraints,
            // if constraints don't affect the entropy of the cell, move on to the next cell
            if (!ConstrainCell(currentCell + direction, constraints)) continue;

            // else...
            // ...update the stack
            propagationStack.Push(currentCell + direction);

            // ...store the state of the cell for eventual backtracks
            if (modifiedEntropies.All(entropy => entropy.pos != cellEntropy.pos))
                modifiedEntropies.Add(cellEntropy);

            // ... and potentially lock the cell if the entropy is 0
            switch (GetCell(currentCell + direction).Count)
            {
                case 1:
                {
                    int protoID = LockEntropy(currentCell + direction);
                    collapsedCells.Add(new(currentCell + direction, protoID, null));
                    break;
                }
                case <= 0:
                    Debug.LogWarning("Propagation produced a null entropy. Backtrack is necessary !");
                    return false;
            }
        }

        return true;
    }

    private GameObject GenerateModuleInstance(Vector3Int currentCell, int chosenState)
    {
        Prototype proto = _prototypeLut.GetPrototype(chosenState);
        GameObject go = Instantiate(proto.prefab,
            new Vector3(currentCell.x, currentCell.y, currentCell.z) * ModuleSockets.MODULE_SIZE,
            Quaternion.Euler(0, proto.angle * 90, 0),
            SpawnedModulesContainer);
        go.name = $"Module ({currentCell.x} {currentCell.y} {currentCell.z})";
        if (go.TryGetComponent(out ModuleSockets mode))
        {
            if (Application.isEditor && !Application.isPlaying)
                DestroyImmediate(mode);
            else
                Destroy(mode);
        }

        foreach (var contentLoader in go.GetComponentsInChildren<VariableContent>())
            contentLoader.ShowContent();

        return go;
    }

    private int ProcessIterationResult(IterationResult context)
    {
        switch (context.result)
        {
            case CollapseStatus.nullEntropy:
            //Debug.LogWarning("Generation was interrupted due to a null Entropy on a cell.");
            case CollapseStatus.infiniteLoop:
            {
                //Debug.LogWarning("Generation was interrupted due to an infinite loop while propagating collapse constraits.");

                if (_generationHistory.Count > 0)
                {
                    _generationHistory.UndoLastEntryGeneration(ref _grid);

                    while (_generationHistory.Peek().remainingCells.Count == 0)
                    {
                        _generationHistory.Backtrack(ref _grid);
                        //iterator--;
                    }

                    return 0;
                }
                else
                {
                    Initialize();
                    return 1;
                }
            }
            case CollapseStatus.singleCollapsed:
            {
                // TODO : stack this step generation with the previous one
                _generationHistory.ExtendLastEntry(
                    context.modifiedCellEntropies,
                    context.generatedInstances);
                return 1;
            }
            case CollapseStatus.randomCollapse:
            {
                // TODO : create new step generation entry
                if (generationHistoryStep == 0)
                {
                    _generationHistory.UpdateLastEntry(
                        _grid,
                        context.modifiedCellEntropies,
                        context.collapsedCell.position,
                        context.collapsedCell.prototypeID,
                        context.generatedInstances);
                }
                else
                {
                    _generationHistory.AddEntry(
                        _grid,
                        context.cellsWithLowestEntropy,
                        context.modifiedCellEntropies,
                        context.collapsedCell.position,
                        context.collapsedCell.prototypeID,
                        context.generatedInstances);
                }

                return 1;
            }
            default:
            {
                return 0;
            }
        }
    }


    private bool ConstrainCell(Vector3Int cell, List<int> constraints)
    {
        bool entropyWasReduce = false;
        foreach (var possibleState in GetCell(cell).ToArray())
            if (!constraints.Contains(possibleState))
            {
                _grid[cell.x, cell.y, cell.z].Remove(possibleState);
                entropyWasReduce = true;
            }

        return entropyWasReduce;
    }

    private float GetEntropy(Vector3Int cell)
    {
        if (GetCell(cell).Count == 0) return -1;
        
        if (!useWeight || _entropyCalculation == EntropyMode.Simple)
        {
            return GetCell(cell).Count - 1;
        }

        if (_entropyCalculation == EntropyMode.Default)
        {
            List<int> weights = GetCell(cell);
            float log2 = Mathf.Log10(2);
            float weightedSum = weights.Sum(proto => _prototypeLut.GetPrototype(proto).weight);
            float entropy = -weights.Sum(proto =>
            {
                float proportionalPrototypeWeight = _prototypeLut.GetPrototype(proto).weight / weightedSum;
                return proportionalPrototypeWeight * Mathf.Log10(proportionalPrototypeWeight) / log2;
            });
            return entropy;
        }

        return 1;
    }

    private bool IsMapGenerated()
    {
        return _nbTiles == 0;
    }


    private List<Vector3Int> GetAdjacentCells(Vector3Int cell)
    {
        List<Vector3Int> positions = new List<Vector3Int>();

        if (cell.x + 1 < worldSize && _grid[cell.x + 1, cell.y, cell.z].Count > 1)
            positions.Add(cell + Vector3Int.right);
        if (cell.x - 1 >= 0 && _grid[cell.x - 1, cell.y, cell.z].Count > 1)
            positions.Add(cell - Vector3Int.right);

        if (cell.y + 1 < worldHeight && _grid[cell.x, cell.y + 1, cell.z].Count > 1)
            positions.Add(cell + Vector3Int.up);
        if (cell.y - 1 >= 0 && _grid[cell.x, cell.y - 1, cell.z].Count > 1)
            positions.Add(cell - Vector3Int.up);

        if (cell.z + 1 < worldSize && _grid[cell.x, cell.y, cell.z + 1].Count > 1)
            positions.Add(cell + Vector3Int.forward);
        if (cell.z - 1 >= 0 && _grid[cell.x, cell.y, cell.z - 1].Count > 1)
            positions.Add(cell - Vector3Int.forward);

        return positions;
    }


#if UNITY_EDITOR
    //debug draw world area
    private void OnDrawGizmos()
    {
        if (!_drawDebug) return;
        if (!_drawBoundaries) return;

        Vector3 cubeDimension = new Vector3(worldSize, worldHeight, worldSize) * ModuleSockets.MODULE_SIZE;
        Gizmos.DrawWireCube(cubeDimension / 2f - Vector3.one * ModuleSockets.MODULE_SIZE / 2f, cubeDimension);
    }

    private void OnDrawGizmosSelected()
    {
        if (!_drawDebug) return;
        if (!_drawEntropies) return;


        if (_grid != null && _grid.Length == worldSize * worldHeight * worldSize)
        {
            for (int z = 0; z < worldSize; z++)
            for (int y = 0; y < worldHeight; y++)
            for (int x = 0; x < worldSize; x++)
            {
                var pos = new Vector3Int(x, y, z);
                float entropy = GetEntropy(pos);
                SortedGizmos.color = entropy < 0 ? Color.red : Color.white;
                SortedGizmos.DrawSphere((Vector3)pos * ModuleSockets.MODULE_SIZE,
                    Mathf.Clamp(Remap(1, 10f, .1f, 1f, entropy), .5f, 1.25f));
            }
        }

        SortedGizmos.BatchCommit();
    }

    float Remap(float inMin, float inMax, float outMin, float outMax, float t) =>
        Mathf.Lerp(outMin, outMax, Mathf.InverseLerp(inMin, inMax, t));
    //outMin + (t - inMin) * (outMax - outMin) / (inMax - inMin);

#endif
}