// wolvenkit.d.ts

declare enum OpenAs {
    GameFile,
    CR2W,
    Json
}

declare enum WMessageBoxImage {
    None,
    Hand,
    Question,
    Exclamation,
    Asterisk,
    Stop,
    Error,
    Warning,
    Information
}

declare enum WMessageBoxButtons {
    OK,
    OKCancel,
    YesNo,
    YesNoCancel
}

declare enum WMessageBoxResult {
    None,
    OK,
    Cancel,
    Yes,
    No
}

interface IDocumentWrapper {
    /** Path of the document */
    readonly path: string;
    /** Returns true if the document has been modified */
    readonly isDirty: boolean;
    /** Saves the document */
    save(): void;
    /** Closes the document */
    close(): void;
}

interface ScriptObject {
    [key: string]: any;
}

interface AppScriptFunctions {
    /**
     * Turn on/off updates to the project tree, useful for when making lots of changes to the project structure.
     * @param suspend - bool for if updates are suspended
     * @deprecated
     */
    SuspendFileWatcher(suspend: boolean): void;

    /**
     * Save the specified CR2WFile or IGameFile to the project.
     * @param path - The file to write to
     * @param file - CR2WFile or IGameFile to be saved
     */
    SaveToProject(path: string, file: object): void;

    /**
     * Save the specified text to the specified path in the raw folder
     * @param path - The file to write to
     * @param content - The string to write to the file
     */
    SaveToRaw(path: string, content: string): void;

    /**
     * Save the specified text to the specified path in the resources folder
     * @param path - The file to write to
     * @param content - The string to write to the file
     */
    SaveToResources(path: string, content: string): void;

    /**
     * Loads the content of a text file from resources
     * @param path - The relative path of the text file
     * @returns The content or null
     */
    LoadFromResources(path: string): string | null;

    /**
     * Loads the specified game file from the project files rather than game archives.
     * @param path - The file to open for reading
     * @param type - The type of the object which is returned. Can be "cr2w" or "json"
     */
    LoadGameFileFromProject(path: string, type: string): object | null;

    /**
     * Loads the specified json file from the project raw files rather than game archives.
     * @param path - The file to open for reading
     * @param type - The type of the object which is returned. Can be "cr2w" or "json"
     */
    LoadRawJsonFromProject(path: string, type: string): object | string | null;

    /**
     * Retrieves a list of files from the project
     * @param folderType - string parameter folderType = "archive" or "raw"
     */
    GetProjectFiles(folderType: string): string[];

    /**
     * Exports a list of files as you would with the export tool.
     * @param fileList - List of files to export
     * @param defaultSettings - Export settings object
     */
    ExportFiles(fileList: any[], defaultSettings?: ScriptObject): void;

    /**
     * Loads a file from the project using either a file path or hash
     * @param path - The path of the file to retrieve
     * @param openAs - The output format (OpenAs.GameFile, OpenAs.CR2W or OpenAs.Json)
     */
    GetFileFromProject(path: string, openAs: OpenAs): object | null;

    /**
     * Loads a file from the project using either a file path or hash
     * @param hash - The hash of the file to retrieve
     * @param openAs - The output format (OpenAs.GameFile, OpenAs.CR2W or OpenAs.Json)
     */
    GetFileFromProject(hash: number, openAs: OpenAs): object | null;

    /**
     * Loads a file from the project or archive (in this order) using either a file path or hash
     * @param path - The path of the file to retrieve
     * @param openAs - The output format (OpenAs.GameFile, OpenAs.CR2W or OpenAs.Json)
     */
    GetFile(path: string, openAs: OpenAs): object | null;

    /**
     * Loads a file from the project or archive (in this order) using either a file path or hash
     * @param hash - The hash of the file to retrieve
     * @param openAs - The output format (OpenAs.GameFile, OpenAs.CR2W or OpenAs.Json)
     */
    GetFile(hash: number, openAs: OpenAs): object | null;

    /**
     * Check if file exists in the project
     * @param path - file path to check
     */
    FileExistsInProject(path: string): boolean;

    /**
     * Check if file exists in the project
     * @param hash - hash value to be checked
     */
    FileExistsInProject(hash: number): boolean;

    /**
     * Check if file exists in either the game archives or the project
     * @param path - file path to check
     */
    FileExists(path: string): boolean;

    /**
     * Check if file exists in either the game archives or the project
     * @param hash - hash value to be checked
     */
    FileExists(hash: number): boolean;

    /**
     * Check if file exists in the project Raw folder
     * @param filepath - relative filepath to be checked
     */
    FileExistsInRaw(filepath: string): boolean;

    /** Loads all records as TweakDBID paths. */
    GetRecords(): string[];

    /** Loads all flats as TweakDBID paths. */
    GetFlats(): string[];

    /** Loads all queries as TweakDBID paths. */
    GetQueries(): string[];

    /** Loads all group tags as TweakDBID paths. */
    GetGroupTags(): string[];

    /**
     * Loads a record by its TweakDBID path.
     * @returns record as a JSON string, null when not found
     */
    GetRecord(path: string): string | null;

    /**
     * Loads a flat by its TweakDBID path.
     * @returns flat as a JSON string, null when not found
     */
    GetFlat(path: string): string | null;

    /**
     * Loads flats of a query by its TweakDBID path.
     * @returns a list of flats as TweakDBID paths, empty when not found
     */
    GetQuery(path: string): string[];

    /**
     * Loads a group tag by its TweakDBID path.
     * @returns flat as a JSON string, null when not found
     */
    GetGroupTag(path: string): number | null;

    /**
     * Whether TweakDBID path exists as a flat or a record?
     */
    HasTDBID(path: string): boolean;

    /**
     * Tries to get TweakDBID path from its hash.
     * @returns path of the hash, null when undefined
     */
    GetTDBIDPath(key: number): string | null;

    /**
     * Displays a message box
     * @param text - A string that specifies the text to display.
     * @param caption - A string that specifies the title bar caption to display.
     * @param image - A WMessageBoxImage value that specifies the icon to display.
     * @param buttons - A WMessageBoxButtons value that specifies which buttons to display.
     * @returns A WMessageBoxResult value that specifies which button was clicked.
     */
    ShowMessageBox(text: string, caption: string, image: WMessageBoxImage, buttons: WMessageBoxButtons): WMessageBoxResult;

    /**
     * Extracts a file from the base archive and adds it to the project
     * @param path - Path of the game file
     */
    Extract(path: string): void;

    /**
     * Gets the current active document from the docking manager
     */
    GetActiveDocument(): IDocumentWrapper | null;

    /**
     * Gets all documents from the docking manager
     */
    GetDocuments(): IDocumentWrapper[] | null;

    /**
     * Opens a file in WolvenKit
     * @param path - Path to the file
     * @returns Returns true if the file was opened, otherwise it returns false
     */
    OpenDocument(path: string): boolean;

    /**
     * Opens an archive game file
     * @param gameFile - The game file to open
     */
    OpenDocument(gameFile: object): void;

    /**
     * Exports an geometry_cache entry
     * @param sectorHashStr - Sector hash as string
     * @param entryHashStr - Entry hash as string
     */
    ExportGeometryCacheEntry(sectorHashStr: string, entryHashStr: string): string | null;

    /**
     * Creates a new instance of the given class, and returns it converted to a JSON string
     * @param className - Name of the class
     */
    CreateInstanceAsJSON(className: string): object | null;

    /**
     * Returns the hashcode for a given string
     * @param data - String to be hashed
     * @param method - Hash method to use. Can be "fnv1a64" or "default" (Uses the String objects built in hash function)
     */
    HashString(data: string, method: string): number | null;

    /**
     * Pauses the execution of the script for the specified amount of milliseconds.
     * @param milliseconds - The number of milliseconds to sleep.
     */
    Sleep(milliseconds: number): void;
}

declare const wkit: AppScriptFunctions;

declare module 'Logger.wscript' {
    /**
     * Logs an Info message to the log
     * @param message - The message to log
     */
    export function Info(message: string | object | null | undefined): void;

    /**
     * Logs a Warning message to the log
     * @param message - The message to log
     */
    export function Warning(message: string | object | null | undefined): void;

    /**
     * Logs an Error message to the log
     * @param message - The message to log
     */
    export function Error(message: string | object | null | undefined): void;

    /**
     * Logs a Debug message to the log
     * @param message - The message to log
     */
    export function Debug(message: string | object | null | undefined): void;

    /**
     * Logs a Success message to the log
     * @param message - The message to log
     */
    export function Success(message: string | object | null | undefined): void;
}
