using UnityEngine;

public struct IterationResult
{
    public CollapseStatus result;
    public CellEntropy[] modifiedCellEntropies;
    public CollapseInfo collapsedCell;
    public CollapseInfo[] propagationCollapsedCells;
    public Vector3Int[] cellsWithLowestEntropy;
    public GameObject[] generatedInstances;
    public bool retry;
}
