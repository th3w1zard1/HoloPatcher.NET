using System.Collections.Generic;
using TSLPatcher.Core.Logger;
using TSLPatcher.Core.Memory;

namespace TSLPatcher.Core.Mods
{
    /// <summary>
    /// Base class for all TSLPatcher modification types.
    /// Represents a modification that can be applied to game resources.
    /// </summary>
    public abstract class PatcherModifications
    {
        public const string DefaultDestination = "Override";

        protected PatcherModifications(string sourceFile, bool? replace = null)
        {
            SourceFile = sourceFile;
            SourceFolder = ".";
            SaveAs = sourceFile;
            ReplaceFile = replace ?? false;
            Destination = DefaultDestination;
            Action = "Patching";
            OverrideType = "warn";
            SkipIfNotReplace = false;
        }

        /// <summary>
        /// The source file for the modifications (!SourceFile)
        /// </summary>
        public string SourceFile { get; set; }

        /// <summary>
        /// The source folder relative to tslpatchdata (!SourceFolder)
        /// </summary>
        public string SourceFolder { get; set; }

        /// <summary>
        /// The final name of the file this patch will save as (!SaveAs or !Filename)
        /// </summary>
        public string SaveAs { get; set; }

        /// <summary>
        /// Whether to replace the file (from Replace#= syntax or !ReplaceFile)
        /// </summary>
        public bool ReplaceFile { get; set; }

        /// <summary>
        /// The destination for the patch file (!Destination)
        /// </summary>
        public string Destination { get; set; }

        /// <summary>
        /// The action for this patch (used for logging purposes)
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// The override type (!OverrideType: warn, ignore, or rename)
        /// </summary>
        public string OverrideType { get; set; }

        /// <summary>
        /// Determines how !ReplaceFile will be handled in InstallList/CompileList
        /// </summary>
        public bool SkipIfNotReplace { get; set; }

        /// <summary>
        /// Patches the resource and returns the modified bytes, or null to skip.
        /// </summary>
        public abstract byte[]? PatchResource(
            byte[] source,
            PatcherMemory memory,
            PatchLogger logger,
            Game game);

        /// <summary>
        /// Parses optional TSLPatcher exclamation-point variables from INI sections.
        /// </summary>
        public virtual void PopTslPatcherVars(
            Dictionary<string, string> sectionDict,
            string? defaultDestination = null,
            string defaultSourceFolder = ".")
        {
            if (sectionDict.TryGetValue("!SourceFile", out var sourceFile))
                SourceFile = sourceFile;

            if (sectionDict.TryGetValue("!ReplaceFile", out var replaceFile))
                ReplaceFile = ConvertToBool(replaceFile);

            if (sectionDict.TryGetValue("!SaveAs", out var saveAs))
                SaveAs = saveAs;

            if (sectionDict.TryGetValue("!Filename", out var filename))
                SaveAs = filename;

            if (sectionDict.TryGetValue("!Destination", out var destination))
                Destination = destination;
            else if (defaultDestination != null)
                Destination = defaultDestination;

            if (sectionDict.TryGetValue("!OverrideType", out var overrideType))
                OverrideType = overrideType.ToLower();

            if (sectionDict.TryGetValue("!SourceFolder", out var sourceFolder))
                SourceFolder = sourceFolder;
            else
                SourceFolder = defaultSourceFolder;
        }

        protected static bool ConvertToBool(string value)
        {
            if (bool.TryParse(value, out var result))
                return result;

            if (int.TryParse(value, out var intValue))
                return intValue != 0;

            return value.Trim().ToLower() == "true";
        }
    }
}
