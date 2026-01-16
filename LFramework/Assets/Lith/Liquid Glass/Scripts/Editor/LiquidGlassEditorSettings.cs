using UnityEditor;

namespace Lith.LiquidGlass
{
    [FilePath("ProjectSettings/LiquidGlassEditorSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class LiquidGlassEditorSettings : ScriptableSingleton<LiquidGlassEditorSettings>
    {
        public bool hdrpAsked = false;
        public bool urpAsked = false;

        public void Save() => Save(true);
    }
}