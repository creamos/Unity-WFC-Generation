using UnityEngine;
public struct CollapseInfo
{
    public Vector3Int position;
    public int prototypeID;
    public GameObject instance;

    public CollapseInfo (Vector3Int position, int prototypeID, GameObject instance)
    {
        this.position = position;
        this.prototypeID = prototypeID;
        this.instance = instance;
    }
}
