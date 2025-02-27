using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeValueCommand<T> : IDirtyCommand
{
    private T originalValue;
    private readonly T value;
    private readonly ConfigField<T> field;
    private readonly Action<string> failureCallback;
    private readonly bool skipEqual;

    public ChangeValueCommand(T value, ConfigField<T> field, Action<string> failureCallback, bool skipEqual = false)
    {
        this.value = value;
        this.field = field;
        this.failureCallback = failureCallback;
        this.skipEqual = skipEqual;
    }

    public bool Execute(ProjectContext context)
    {
        try
        {
            originalValue = field.GetField(context);
            if (!skipEqual && originalValue != null)
            {
                if (originalValue.Equals(value))
                    return false;
            }
            else if (value == null) return false;

            if (skipEqual)
                field.SetField(context, value);
            else if (field.SetField(context, value).Equals(originalValue))
                return false;

            return true;
        }
        catch (Exception e)
        {
            failureCallback.Invoke(e.Message);
            Debug.LogException(e);
            return false;
        }
    }

    public void Undo(ProjectContext context)
    {
        field.SetField(context, originalValue);
    }
}
