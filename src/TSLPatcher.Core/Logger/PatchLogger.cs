using System;
using System.Collections.Generic;
using System.Linq;

namespace TSLPatcher.Core.Logger;

/// <summary>
/// Logger for tracking patch operations
/// </summary>
public class PatchLogger
{
    private readonly List<PatchLog> _allLogs = new();
    private readonly object _lockObject = new();

    public IReadOnlyList<PatchLog> AllLogs
    {
        get
        {
            lock (_lockObject)
            {
                return _allLogs.AsReadOnly();
            }
        }
    }

    public IEnumerable<PatchLog> VerboseLogs => AllLogs.Where(log => log.LogType == LogType.Verbose);
    public IEnumerable<PatchLog> Notes => AllLogs.Where(log => log.LogType == LogType.Note);
    public IEnumerable<PatchLog> Warnings => AllLogs.Where(log => log.LogType == LogType.Warning);
    public IEnumerable<PatchLog> Errors => AllLogs.Where(log => log.LogType == LogType.Error);

    public int PatchesCompleted { get; private set; }

    // Events for real-time log updates
    public event EventHandler<PatchLog>? LogAdded;
    public event EventHandler<PatchLog>? VerboseLogged;
    public event EventHandler<PatchLog>? NoteLogged;
    public event EventHandler<PatchLog>? WarningLogged;
    public event EventHandler<PatchLog>? ErrorLogged;

    public void CompletePatch()
    {
        PatchesCompleted++;
    }

    public void AddVerbose(string message)
    {
        var log = new PatchLog(message, LogType.Verbose);
        lock (_lockObject)
        {
            _allLogs.Add(log);
        }
        LogAdded?.Invoke(this, log);
        VerboseLogged?.Invoke(this, log);
    }

    public void AddNote(string message)
    {
        var log = new PatchLog(message, LogType.Note);
        lock (_lockObject)
        {
            _allLogs.Add(log);
        }
        LogAdded?.Invoke(this, log);
        NoteLogged?.Invoke(this, log);
    }

    public void AddWarning(string message)
    {
        var log = new PatchLog(message, LogType.Warning);
        lock (_lockObject)
        {
            _allLogs.Add(log);
        }
        LogAdded?.Invoke(this, log);
        WarningLogged?.Invoke(this, log);
    }

    public void AddError(string message)
    {
        var log = new PatchLog(message, LogType.Error);
        lock (_lockObject)
        {
            _allLogs.Add(log);
        }
        LogAdded?.Invoke(this, log);
        ErrorLogged?.Invoke(this, log);
    }

    public void Clear()
    {
        lock (_lockObject)
        {
            _allLogs.Clear();
            PatchesCompleted = 0;
        }
    }
}

