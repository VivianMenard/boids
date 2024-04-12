using UnityEngine;
using System.Reflection;

public class ShadersSharedParameters : MonoBehaviour
{
    [SerializeField]
    private WaterMovementsFloatParams waterMovementsFloatParams;

    private void Awake()
    {
        SetGlobalFloatParams();
    }
    private void OnValidate()
    {
        SetGlobalFloatParams();
    }

    private void SetGlobalFloatParams()
    {
        FieldInfo[] fields = waterMovementsFloatParams.GetType().GetFields();

        foreach (FieldInfo field in fields)
        {
            float value = (float)field.GetValue(waterMovementsFloatParams);
            Shader.SetGlobalFloat("_" + field.Name, value);
        }
    }
}
