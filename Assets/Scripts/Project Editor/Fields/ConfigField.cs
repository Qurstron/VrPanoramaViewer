using Newtonsoft.Json.Bson;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public abstract class ConfigField<T> : ScriptableObject
{
    [SerializeField]
    private FieldUpdateLevel updateLevel;
    public FieldUpdateLevel UpdateLevel
    {
        get { return updateLevel; }
    }

    /// <returns>the actual value of the field</returns>
    public abstract T SetField(ProjectContext context, T valueSuggestion);
    public abstract T GetField(ProjectContext context);
    /// <summary>
    /// Determines if the field accepts an input
    /// </summary>
    public virtual bool IsInputReady(ProjectContext context)
    {
        return true;
    }

    public UnityEvent<T> onFieldChange = new();
}

public enum FieldUpdateLevel
{
    Config,
    Node,
    NodeContent,
    Object,
    Selection
}
