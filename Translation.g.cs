using RT.Util.Lingo;

namespace TankIconMaker
{
    [LingoStringClass, LingoInGroup(TranslationGroup.MainWindow)]
    sealed partial class MainWindowTranslation
    {
        // The following control was not added because it has no name: Tank Icon Maker

        [LingoAutoGenerated]
        public TrString ctGameDirLabel = "Game _directory:";

        [LingoAutoGenerated]
        public TrString ctIconStyleLabel = "_Icon style:";

        [LingoAutoGenerated]
        public TrString ctStyleNewLabel = "_New";

        [LingoAutoGenerated]
        public TrString ctStyleImportLabel = "Im_port";

        [LingoAutoGenerated]
        public TrString ctStyleExportLabel = "E_xport";

        [LingoAutoGenerated]
        public TrString ctUpvote = "Upvote the author of this style";

        [LingoAutoGenerated]
        public TrString ctStyleMoreLabel = "_More";

        [LingoAutoGenerated]
        public TrString ctStyleIconWidth = "Icon width";

        [LingoAutoGenerated]
        public TrString ctStyleIconHeight = "Icon height";

        [LingoAutoGenerated]
        public TrString ctStyleCenterable = "Centerable";

        [LingoAutoGenerated]
        public TrString ctStyleChangeName = "Change _name";

        [LingoAutoGenerated]
        public TrString ctStyleChangeAuthor = "Change _author";

        [LingoAutoGenerated]
        public TrString ctStyleDuplicate = "_Duplicate";

        [LingoAutoGenerated]
        public TrString ctStyleDelete = "D_elete";

        [LingoAutoGenerated]
        public TrString ctLayersLabel = "_Layers:";

        [LingoAutoGenerated]
        public TrString ctLayersAddLayer = "Add _layer...";

        [LingoAutoGenerated]
        public TrString ctLayersAddEffect = "Add _effect...";

        [LingoAutoGenerated]
        public TrString ctLayersCopy = "_Copy";

        [LingoAutoGenerated]
        public TrString ctLayersCopyEffects = "Copy e_ffects";

        [LingoAutoGenerated]
        public TrString ctLayersPaste = "_Paste";

        [LingoAutoGenerated]
        public TrString ctLayersDelete = "_Delete";

        [LingoAutoGenerated]
        public TrString ctLayersRename = "_Rename";

        [LingoAutoGenerated]
        public TrString ctLayersMoveUp = "Move _up";

        [LingoAutoGenerated]
        public TrString ctLayersMoveDown = "Move d_own";

        [LingoAutoGenerated]
        public TrString ctLayersToggleVisibility = "Toggle _visibility";

        [LingoAutoGenerated]
        public TrString ctZoomCheckbox = "_Zoom";

        [LingoAutoGenerated]
        public TrString ctDisplayModeAll = "All tanks";

        [LingoAutoGenerated]
        public TrString ctDisplayModeOneEach = "One of each combination";

        [LingoAutoGenerated]
        public TrString ctDisplayModeCountryUSSR = "USSR";

        [LingoAutoGenerated]
        public TrString ctDisplayModeCountryGermany = "Germany";

        [LingoAutoGenerated]
        public TrString ctDisplayModeCountryUSA = "USA";

        [LingoAutoGenerated]
        public TrString ctDisplayModeCountryFrance = "France";

        [LingoAutoGenerated]
        public TrString ctDisplayModeCountryUK = "UK";

        [LingoAutoGenerated]
        public TrString ctDisplayModeCountryChina = "China";

        [LingoAutoGenerated]
        public TrString ctDisplayModeCountryJapan = "Japan";

        [LingoAutoGenerated]
        public TrString ctDisplayModeClassLight = "Light tanks";

        [LingoAutoGenerated]
        public TrString ctDisplayModeClassMedium = "Medium tanks";

        [LingoAutoGenerated]
        public TrString ctDisplayModeClassHeavy = "Heavy tanks";

        [LingoAutoGenerated]
        public TrString ctDisplayModeClassArtillery = "Artillery";

        [LingoAutoGenerated]
        public TrString ctDisplayModeClassDestroyer = "Tank destroyers";

        [LingoAutoGenerated]
        public TrString ctDisplayModeCategoryNormal = "Normal";

        [LingoAutoGenerated]
        public TrString ctDisplayModeCategoryPremium = "Premium";

        [LingoAutoGenerated]
        public TrString ctDisplayModeCategorySpecial = "Special";

        [LingoAutoGenerated]
        public TrString ctDisplayModeTierLow = "Low tiers";

        [LingoAutoGenerated]
        public TrString ctDisplayModeTierMedHigh = "Medium and high tiers";

        [LingoAutoGenerated]
        public TrString ctDisplayModeTierHigh = "High tiers";

        [LingoAutoGenerated]
        public TrString ctLanguageLabel = "Lang_uage";

        [LingoAutoGenerated]
        public TrString ctBackgroundLabel = "_Background";

        [LingoAutoGenerated]
        public TrString ctWarning = "Indicates that a problem has occurred. Click this icon to view details of the problem.";

        [LingoAutoGenerated]
        public TrString ctSaveLabel = "_Save";

        [LingoAutoGenerated]
        public TrString ctSaveAsLabel = "Save _as...";

        [LingoAutoGenerated]
        public TrString ctSaveIconsToGameFolder = "Save to _game folder";

        [LingoAutoGenerated]
        public TrString ctSaveIconsToSpecifiedFolder = "Save to _folder...";

        [LingoAutoGenerated]
        public TrString ctBulkSaveIcons = "Bulk _save";

        [LingoAutoGenerated]
        public TrString ctPathTemplateLabel = "Path:";

        [LingoAutoGenerated]
        public TrString ctEditPathTemplateLabel = "_...";

        [LingoAutoGenerated]
        public TrString ctReloadLabel = "_Reload data";

        [LingoAutoGenerated]
        public TrString ctAboutLabel = "About...";
    }

    [LingoStringClass, LingoInGroup(TranslationGroup.PathTemplateWindow)]
    sealed partial class PathTemplateWindowTranslation
    {
        [LingoAutoGenerated]
        public TrString ctHelp = "This template specifies which path the icons are to be saved to. It is relative to the selected game installation directory. Leave blank to save the icons to the standard location, which works in an unmodified World of Tanks client and requires no mods. Any non-standard paths will only have an effect if you have the appropriate mods installed.";

        [LingoAutoGenerated]
        public TrString ctExpandsToLbl = "Preview:";

        [LingoAutoGenerated]
        public TrString ctTemplateElements = "Use the following template elements to construct the path:";

        // The following control was not added because it has no name: {ModsPath}

        [LingoAutoGenerated]
        public TrString ctModsPathHelp = "Full path to the res_mods\\... subfolder for the currently selected game version.\nCurrent value: “{cur}”";

        // The following control was not added because it has no name: {TimPath}

        [LingoAutoGenerated]
        public TrString ctTimPathHelp = "Full path to the folder containing Tank Icon Maker.\nCurrent value: “{cur}”";

        // The following control was not added because it has no name: {GamePath}

        [LingoAutoGenerated]
        public TrString ctGamePathHelp = "Full path to the location where World of Tanks is installed.\nCurrent value: “{cur}”";

        // The following control was not added because it has no name: {GameVersion}

        [LingoAutoGenerated]
        public TrString ctGameVersionHelp = "Version identifier for the selected WoT folder. Examples: “0.9.5”, “0.9.5 Common Test”.\nCurrent value: “{cur}”";

        // The following control was not added because it has no name: {TankClass}

        [LingoAutoGenerated]
        public TrString ctTankClassHelp = "Specifies the tank class for a given icon (heavy, medium etc.). Varies for each icon.";

        // The following control was not added because it has no name: {TankCountry}

        [LingoAutoGenerated]
        public TrString ctTankCountryHelp = "Specifies the tank country for a given icon. Varies for each icon.";

        // The following control was not added because it has no name: {StyleName}

        [LingoAutoGenerated]
        public TrString ctStyleNameHelp = "The name of the selected style. Current value: “{cur}”";

        // The following control was not added because it has no name: {StyleAuthor}

        [LingoAutoGenerated]
        public TrString ctStyleAuthorHelp = "The author of the selected style. Current value: “{cur}”";

        // The following control was not added because it has no name: %UserProfile%\n%AppData%\n...

        [LingoAutoGenerated]
        public TrString ctEnvVarsHelp = "All environment variables are expanded to their values. %UserProfile% expands to the user's profile folder path. %AppData% expands to the primary folder containing application settings. Many other environment variables are documented in online sources.";

        [LingoAutoGenerated]
        public TrString ctOkBtn = "_OK";

        [LingoAutoGenerated]
        public TrString ctCancelBtn = "_Cancel";
    }
}
