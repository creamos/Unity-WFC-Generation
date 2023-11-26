using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GenerationTrackList
{
    public struct Entry
    {
        public List<Vector3Int> remainingCells;
        public List<List<int>> remainingEntropies;
        public List<GameObject> generatedInstances;
        public List<CellEntropy> modifiedCellEntropies;
    }

    private CircularStack<Entry> entries;

    public GenerationTrackList (uint bufferSize)
    {
        entries = new CircularStack<Entry>(bufferSize);
    }

    public int Count => entries.Count;

    public Entry Peek () => entries.Peek();

    public void AddEntry (List<int>[,,] grid, Vector3Int[] lowestEntropyCells, CellEntropy[] modifiedCellEntropies, Vector3Int chosenCell, int chosenPrototypeID, GameObject[] generatedInstances)
    {
        // Create a list that, for each given cell, contains its current entropy,
        // and remove the chosen prototype from the entropy of the chosen cell
        List<List<int>> entropies = lowestEntropyCells
            .Select(cell => {
                if (cell == chosenCell)
                    return grid[cell.x,cell.y,cell.z]
                    .Where(prototypeID => prototypeID != chosenPrototypeID)
                    .ToList();
                else
                    return grid[cell.x,cell.y,cell.z];
            })
            .ToList();

        // Filter the remaining cells by removing the ones with a null entropy
        List<Vector3Int> remainingCells = lowestEntropyCells
            .Where((cell, id) => entropies[id].Count > 0 && chosenCell != cell)
            .ToList();

        // Filter the entropy list the same way
        entropies = entropies
            .Where(entropy => entropy.Count > 0)
            .ToList();

        // Create the entry with the produced data
        entries.Push(new Entry {
            remainingCells = remainingCells,
            modifiedCellEntropies = modifiedCellEntropies.ToList(),
            remainingEntropies = entropies,
            generatedInstances = generatedInstances.ToList()
        });
    }

    public void UpdateLastEntry(Vector3Int chosenCell)
    {
        var lastEntry = entries.Peek();
        var cellID = lastEntry.remainingCells.IndexOf(chosenCell);
        
        if (cellID == -1) return;
        
        lastEntry.remainingCells.RemoveAt(cellID);
        lastEntry.remainingEntropies.RemoveAt(cellID);
    }

    public void UpdateLastEntry (List<int>[,,] grid, CellEntropy[] modifiedCellEntropies, Vector3Int chosenCell, int chosenPrototypeID, GameObject[] generatedInstances)
    {
        var lastEntry = entries.Pop();

        // Make sure the given cell is part of the remaining ones in this entry
        int indexOfChosenCell = lastEntry.remainingCells.IndexOf(chosenCell);
        if (indexOfChosenCell >= 0) {

            // Remove from its associated entropy the chosen prototype id
            lastEntry.remainingEntropies[indexOfChosenCell].Remove(chosenPrototypeID);

            // Remove the cell & its entropy if the latter is 0
            if (lastEntry.remainingEntropies[indexOfChosenCell].Count == 0) {
                lastEntry.remainingCells.RemoveAt(indexOfChosenCell);
                lastEntry.remainingEntropies.RemoveAt(indexOfChosenCell);
            }
        }

        // Update the list of generated instances
        lastEntry.generatedInstances = generatedInstances.ToList();

        // Stores any newly modified entropies
        var newlyModifiedCellEntropies = modifiedCellEntropies 
            .Where(modifiedCellEntropy => {
                return !lastEntry.modifiedCellEntropies.Any(
                    alreadyModifiedCell =>
                    alreadyModifiedCell.pos == modifiedCellEntropy.pos);
            }).ToList();

        lastEntry.modifiedCellEntropies.AddRange(newlyModifiedCellEntropies);

        entries.Push(lastEntry);
    }

    public void ExtendLastEntry (CellEntropy[] modifiedCellEntropies, GameObject[] generatedInstances)
    {
        var lastEntry = entries.Pop();

        // Update the list of generated instances
        lastEntry.generatedInstances.AddRange(generatedInstances);

        // Stores any newly modified entropies
        var newlyModifiedCellEntropies = modifiedCellEntropies
            .Where(modifiedCellEntropy => {
                return !lastEntry.modifiedCellEntropies.Any(
                    alreadyModifiedCell =>
                    alreadyModifiedCell.pos == modifiedCellEntropy.pos);
            }).ToList();

        lastEntry.modifiedCellEntropies.AddRange(newlyModifiedCellEntropies);

        entries.Push(lastEntry);
    }

    public void UndoLastEntryGeneration (ref List<int>[,,] grid)
    {
        var entry = entries.Peek();
        foreach (var go in entry.generatedInstances) {
            if (go != null)
            {
                if (Application.isPlaying) Object.Destroy(go);
                else Object.DestroyImmediate(go);
            }
        }
        entry.generatedInstances.Clear();

        foreach (var cellEntropy in entry.modifiedCellEntropies)
            grid[cellEntropy.pos.x, cellEntropy.pos.y, cellEntropy.pos.z] = cellEntropy.entropy;

        entry.modifiedCellEntropies.Clear();
    }

    public void Backtrack (ref List<int>[,,] grid)
    {
        UndoLastEntryGeneration(ref grid);
        entries.Pop();
    }
}
