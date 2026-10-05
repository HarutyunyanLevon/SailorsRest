using System;

namespace SailorsRest
{
    /// <summary>
    /// Names the PNG (without extension) a Sprite field is filled from. The scene builder uses it to wire
    /// <see cref="UiSkin"/> and <see cref="WorldArt"/> to the SpriteCook files by name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class SpriteFileAttribute : Attribute
    {
        public string FileName { get; }
        public SpriteFileAttribute(string fileName) => FileName = fileName;
    }
}
