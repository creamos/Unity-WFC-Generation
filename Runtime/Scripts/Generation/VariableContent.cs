using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class VariableContent : MonoBehaviour
{
    [SerializeField, Range(0, 1)] private float _contentSpawnProbability = .1f;
    private bool _showContent;

    private void OnEnable()
    {
        ShowContent();
    }

    public void ShowContent()
    {
        if (_showContent) return;
        _showContent = true;

        var models = transform.GetComponentsInChildren<Transform>(true)
            .Where(ts => ts.parent == transform).ToArray();

        foreach (var model in models.Where(ts => ts.gameObject.activeSelf))
            model.gameObject.SetActive(false);

        if (Random.Range(0f, 1f) <= _contentSpawnProbability)
        {
            var obj = models[Random.Range(0, models.Length)].gameObject;
            obj.SetActive(true);
            if (obj.TryGetComponent(out VariableContent cmp))
                cmp.ShowContent();
        }
    }
}