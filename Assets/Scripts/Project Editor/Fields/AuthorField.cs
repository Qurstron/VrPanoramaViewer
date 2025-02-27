using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AuthorField", menuName = "ConfigFields/AuthorField")]
public class AuthorField : ConfigField<string>
{
    public override string GetField(ProjectContext context)
    {
        return context.Config.author;
    }
    public override string SetField(ProjectContext context, string value)
    {
        context.Config.author = value;
        onFieldChange.Invoke(value);
        return value;
    }
}
