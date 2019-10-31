using System;

namespace Kommon.Qmmands
{
    /// <inheritdoc />
    /// <summary>
    /// Classes marked with this attribute won't replace the primitive type parser.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class DontOverrideAttribute : Attribute
    {
    }
}
