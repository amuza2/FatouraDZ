using System;

namespace FatouraDZ.Services;

/// <summary>
/// Abstraction de journalisation de l'application.
/// </summary>
public interface IAppLogger
{
    void Info(string message);
    void Warning(string message, Exception? exception = null);
    void Error(string message, Exception? exception = null);
}
