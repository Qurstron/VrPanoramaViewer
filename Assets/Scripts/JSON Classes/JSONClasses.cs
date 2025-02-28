using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using static QUtils;

namespace JSONClasses
{
    /// <summary>
    /// Used to Display Panoramaentries in the main menu. It Contains useful metadata for that application.
    /// </summary>
    /// <remarks>
    /// Only the path and error field will always contain a value. If HasError is false, then all fields will contain data.
    /// </remarks>
    public class PanoramaMenuEntry
    {
        public Config config;
        //public string name;
        public string customError;
        public string path; // backup path if config fails to load
        public int nodeCount;
        //public bool isWip;
        public long size;
        //public long version;
        public Texture2D thumbnail;
        public DateTime lastEdited;
        public AppConfig.RecentProject recentProject;
        public Error error = Error.NoError;

        public bool HasError { get { return error != Error.NoError; }}

        public enum Error
        {
            NoError,
            Undefined,
            FileNotFound,
            Deserialize,
            Validation
        }
    }

    /// <summary>
    /// When a config is deserialized it needs to be validated, as well as certain objects it contains.
    /// </summary>
    [Serializable]
    public abstract class Validatable
    {
        /// <summary>
        /// Display name wich is not unique
        /// </summary>
        public string name;
        protected readonly List<string> problems = new();
        private readonly HashSet<Validatable> validatables = new();
        [JsonIgnore] public bool HasProblems
        {
            get { return problems.Count > 0; }
        }

        /// <summary>
        /// This method will be called when a Validatable parent wants to validate its children.
        /// </summary>
        /// <remarks>
        /// Repeated calls should not change the object or add more problems beyond the first call,
        /// if the object was successfully validated.
        /// </remarks>
        protected abstract void OnValidate();
        public void AddProblem(string reason)
        {
            problems.Add(reason);
        }
        /// <summary>
        /// Checks if a string is null or empty and adds a problem if it is.
        /// </summary>
        /// <returns>True if str is not null or empty</returns>
        protected bool ValidateNotNullorEmpty(string str, [CallerArgumentExpression("str")] string strName = null)
        {
            if (string.IsNullOrEmpty(str))
            {
                AddProblem($"String {strName} cannot be null or empty");
                return false;
            }

            return true;
        }
        protected void ValidateColor(ref string color, [CallerArgumentExpression("color")] string colorName = null)
        {
            try
            {
                StringToColor(color);
                color = FormatHexColor(color);
            }
            catch
            {
                AddProblem($"Color {colorName} not validatable");
            }
        }

        public string GetAllProblems()
        {
            return string.Join("\n", SelectProblems());
        }
        private IEnumerable<string> SelectProblems()
        {
            return problems.Select(value => $"{name} - {value}");
        }

        protected void Validate(Validatable validatable, bool preserveProblems = false)
        {
            if (!validatables.Contains(validatable)) validatables.Add(validatable);
            problems.AddRange(validatable.Validate(preserveProblems));
        }
        public List<string> Validate(bool preserveProblems = false)
        {
            if (string.IsNullOrEmpty(name))
            {
                if (this is Object3D obj3D)
                    name = Path.GetFileNameWithoutExtension(obj3D.file);
                else
                    name = GetType().Name;
            }

            if (!preserveProblems) problems.Clear(); // imagen it would be this easy to solve real life problems
            OnValidate();

            return SelectProblems().ToList();
        }

        /// <summary>
        /// Used for cleanup, so values that don't hold any aditional information beyond the default are removed.
        /// </summary>
        public virtual void PrepareSave()
        {
            foreach (Validatable validatable in validatables)
            {
                if (this == validatable) continue;
                validatable.PrepareSave();
            }
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(name)) return base.ToString();
            return name;
        }
    }
}

// https://stackoverflow.com/questions/70034586/how-can-i-use-callerargumentexpression-with-visual-studio-2022-and-net-standard
namespace System.Runtime.CompilerServices
{
#if !NET6_0_OR_GREATER

    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    internal sealed class CallerArgumentExpressionAttribute : Attribute
    {
        public CallerArgumentExpressionAttribute(string parameterName)
        {
            ParameterName = parameterName;
        }

        public string ParameterName { get; }
    }

#endif
}
