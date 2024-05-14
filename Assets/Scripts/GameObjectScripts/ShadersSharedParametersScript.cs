using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
using System;

/// <summary>
/// Allows to easily set global shaders parameters from the inspector.
/// </summary>
public class ShadersSharedParametersScript : MonoBehaviour
{
    [SerializeField]
    private SharedParameters sharedParameters;

    /// <summary>
    /// A dictionary that allows to get the method to set a global shaders parameter from its type.
    /// </summary>
    private Dictionary<Type, Action<string, object>> TypeToSetGlobalVar = new Dictionary<Type, Action<string, object>>
    {
        { typeof(float), (name, value) => Shader.SetGlobalFloat(name, (float)value) },
        { typeof(Vector2), (name, value) => Shader.SetGlobalVector(name, (Vector2)value) },
        { typeof(Vector3), (name, value) => Shader.SetGlobalVector(name, (Vector3)value) },
        { typeof(Color), (name, value) => Shader.SetGlobalColor(name, (Color)value) }
    };

    private void Awake()
    {
        SetGlobalParameters();
    }
    private void OnValidate()
    {
        SetGlobalParameters();
    }

    /// <summary>
    /// Sets in once all the global shaders parameters according to the values fill in the inspector.
    /// </summary>
    private void SetGlobalParameters()
    {
        FieldInfo[] fields = sharedParameters.GetType().GetFields();

        foreach (FieldInfo field in fields)
        {
            Type fieldType = field.FieldType;
            object value = field.GetValue(sharedParameters);
            string fieldName = field.Name;
            string fieldReference = "_" + fieldName;

            if (TypeToSetGlobalVar.ContainsKey(fieldType))
                TypeToSetGlobalVar[fieldType](fieldReference, value);

            else
                Debug.LogWarning(
                    "The SetGlobal method associated to type " +
                    fieldType + " isn't already implemented."
                );
        }
    }
}
