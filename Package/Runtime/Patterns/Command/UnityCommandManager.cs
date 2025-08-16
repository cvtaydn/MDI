using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace MDI.Patterns.Command
{
    /// <summary>
    /// Unity entegre command manager
    /// </summary>
    public class UnityCommandManager : MonoBehaviour
    {
        [Header("Command Manager Settings")]
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private int _maxHistorySize = 100;
        [SerializeField] private bool _autoExecuteOnStart = false;
        
        private static UnityCommandManager _instance;
        private ICommandExecutor _commandExecutor;
        private readonly Queue<IBaseCommand> _commandQueue = new Queue<IBaseCommand>();
        private readonly List<IBaseCommand> _scheduledCommands = new List<IBaseCommand>();
        private bool _isProcessingQueue = false;

        /// <summary>
        /// Singleton instance
        /// </summary>
        public static UnityCommandManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<UnityCommandManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("[UnityCommandManager]");
                        _instance = go.AddComponent<UnityCommandManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Command executor
        /// </summary>
        public ICommandExecutor CommandExecutor => _commandExecutor;

        /// <summary>
        /// Command queue count
        /// </summary>
        public int QueueCount => _commandQueue.Count;

        /// <summary>
        /// Scheduled commands count
        /// </summary>
        public int ScheduledCount => _scheduledCommands.Count;

        /// <summary>
        /// Command executed event
        /// </summary>
        public event Action<IBaseCommand, CommandResult> CommandExecuted;

        /// <summary>
        /// Command failed event
        /// </summary>
        public event Action<IBaseCommand, CommandResult> CommandFailed;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            if (_autoExecuteOnStart)
            {
                StartCoroutine(ProcessCommandQueue());
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _commandExecutor?.ClearHistory();
                _commandQueue.Clear();
                _scheduledCommands.Clear();
                _instance = null;
            }
        }

        /// <summary>
        /// Manager'ı initialize eder
        /// </summary>
        private void Initialize()
        {
            _commandExecutor = new CommandExecutor();
            _commandExecutor.CommandExecuted += OnCommandExecuted;
            _commandExecutor.CommandUndone += OnCommandUndone;

            if (_enableLogging)
            {
                Debug.Log("[UnityCommandManager] Initialized");
            }
        }

        /// <summary>
        /// Command'ı hemen çalıştırır
        /// </summary>
        /// <param name="command">Çalıştırılacak command</param>
        /// <returns>Execution result</returns>
        public CommandResult Execute(IBaseCommand command)
        {
            if (command == null)
            {
                LogError("Cannot execute null command");
                return CommandResult.Failure("Command cannot be null");
            }

            var result = _commandExecutor.Execute(command);
            
            if (_enableLogging)
            {
                if (result.IsSuccess)
                    Log($"Command executed successfully: {command.Name}");
                else
                    LogError($"Command failed: {command.Name} - {result.ErrorMessage}");
            }

            return result;
        }

        /// <summary>
        /// Command'ı asenkron olarak çalıştırır
        /// </summary>
        /// <param name="command">Çalıştırılacak command</param>
        /// <returns>Async execution result</returns>
        public async Task<CommandResult> ExecuteAsync(IBaseCommand command)
        {
            if (command == null)
            {
                LogError("Cannot execute null command");
                return CommandResult.Failure("Command cannot be null");
            }

            var result = await _commandExecutor.ExecuteAsync(command);
            
            if (_enableLogging)
            {
                if (result.IsSuccess)
                    Log($"Async command executed successfully: {command.Name}");
                else
                    LogError($"Async command failed: {command.Name} - {result.ErrorMessage}");
            }

            return result;
        }

        /// <summary>
        /// Command'ı kuyruğa ekler
        /// </summary>
        /// <param name="command">Kuyruğa eklenecek command</param>
        public void Enqueue(IBaseCommand command)
        {
            if (command == null)
            {
                LogError("Cannot enqueue null command");
                return;
            }

            _commandQueue.Enqueue(command);
            
            if (_enableLogging)
                Log($"Command enqueued: {command.Name} (Queue size: {_commandQueue.Count})");

            if (!_isProcessingQueue)
            {
                StartCoroutine(ProcessCommandQueue());
            }
        }

        /// <summary>
        /// Command'ı belirtilen süre sonra çalıştırmak için zamanlar
        /// </summary>
        /// <param name="command">Zamanlanacak command</param>
        /// <param name="delay">Gecikme süresi (saniye)</param>
        public void Schedule(IBaseCommand command, float delay)
        {
            if (command == null)
            {
                LogError("Cannot schedule null command");
                return;
            }

            _scheduledCommands.Add(command);
            StartCoroutine(ExecuteAfterDelay(command, delay));
            
            if (_enableLogging)
                Log($"Command scheduled: {command.Name} (Delay: {delay}s)");
        }

        /// <summary>
        /// Command'ları sıralı olarak çalıştırır
        /// </summary>
        /// <param name="commands">Çalıştırılacak command'lar</param>
        /// <returns>Execution results</returns>
        public IEnumerable<CommandResult> ExecuteSequentially(params IBaseCommand[] commands)
        {
            return _commandExecutor.ExecuteSequentially(commands);
        }

        /// <summary>
        /// Command'ları paralel olarak çalıştırır
        /// </summary>
        /// <param name="commands">Çalıştırılacak command'lar</param>
        /// <returns>Async execution results</returns>
        public async Task<IEnumerable<CommandResult>> ExecuteParallel(params IBaseCommand[] commands)
        {
            return await _commandExecutor.ExecuteParallel(commands);
        }

        /// <summary>
        /// Command'ları koşullu olarak çalıştırır
        /// </summary>
        /// <param name="commands">Çalıştırılacak command'lar</param>
        /// <returns>Execution results</returns>
        public IEnumerable<CommandResult> ExecuteConditional(params IBaseCommand[] commands)
        {
            return _commandExecutor.ExecuteConditional(commands);
        }

        /// <summary>
        /// Son command'ı undo eder
        /// </summary>
        /// <returns>Undo result</returns>
        public CommandResult UndoLast()
        {
            var result = _commandExecutor.UndoLast();
            
            if (_enableLogging)
            {
                if (result.IsSuccess)
                    Log("Last command undone successfully");
                else
                    LogError($"Undo failed: {result.ErrorMessage}");
            }

            return result;
        }

        /// <summary>
        /// Command history'sini temizler
        /// </summary>
        public void ClearHistory()
        {
            _commandExecutor.ClearHistory();
            
            if (_enableLogging)
                Log("Command history cleared");
        }

        /// <summary>
        /// Command kuyruğunu temizler
        /// </summary>
        public void ClearQueue()
        {
            _commandQueue.Clear();
            
            if (_enableLogging)
                Log("Command queue cleared");
        }

        /// <summary>
        /// Zamanlanmış command'ları temizler
        /// </summary>
        public void ClearScheduled()
        {
            _scheduledCommands.Clear();
            
            if (_enableLogging)
                Log("Scheduled commands cleared");
        }

        /// <summary>
        /// Command execution history'si
        /// </summary>
        public IReadOnlyList<IBaseCommand> ExecutionHistory => _commandExecutor.ExecutionHistory;

        #region Private Methods

        private IEnumerator ProcessCommandQueue()
        {
            _isProcessingQueue = true;

            while (_commandQueue.Count > 0)
            {
                var command = _commandQueue.Dequeue();
                var result = Execute(command);
                
                // Bir frame bekle
                yield return null;
            }

            _isProcessingQueue = false;
        }

        private IEnumerator ExecuteAfterDelay(IBaseCommand command, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (_scheduledCommands.Contains(command))
            {
                _scheduledCommands.Remove(command);
                Execute(command);
            }
        }

        private void OnCommandExecuted(IBaseCommand command, CommandResult result)
        {
            if (result.IsSuccess)
            {
                CommandExecuted?.Invoke(command, result);
            }
            else
            {
                CommandFailed?.Invoke(command, result);
            }

            // History size kontrolü
            if (_commandExecutor.ExecutionHistory.Count > _maxHistorySize)
            {
                // En eski command'ları temizle (bu özellik CommandExecutor'a eklenebilir)
                if (_enableLogging)
                    Log($"History size exceeded {_maxHistorySize}, consider clearing old commands");
            }
        }

        private void OnCommandUndone(IBaseCommand command, CommandResult result)
        {
            if (_enableLogging)
            {
                if (result.IsSuccess)
                    Log($"Command undone: {command.Name}");
                else
                    LogError($"Command undo failed: {command.Name} - {result.ErrorMessage}");
            }
        }

        private void Log(string message)
        {
            if (_enableLogging)
                Debug.Log($"[UnityCommandManager] {message}");
        }

        private void LogError(string message)
        {
            if (_enableLogging)
                Debug.LogError($"[UnityCommandManager] {message}");
        }

        #endregion

        #region Static Helper Methods

        /// <summary>
        /// Static helper - Command'ı çalıştırır
        /// </summary>
        /// <param name="command">Çalıştırılacak command</param>
        /// <returns>Execution result</returns>
        public static CommandResult ExecuteCommand(IBaseCommand command)
        {
            return Instance.Execute(command);
        }

        /// <summary>
        /// Static helper - Command'ı kuyruğa ekler
        /// </summary>
        /// <param name="command">Kuyruğa eklenecek command</param>
        public static void EnqueueCommand(IBaseCommand command)
        {
            Instance.Enqueue(command);
        }

        /// <summary>
        /// Static helper - Command'ı zamanlar
        /// </summary>
        /// <param name="command">Zamanlanacak command</param>
        /// <param name="delay">Gecikme süresi (saniye)</param>
        public static void ScheduleCommand(IBaseCommand command, float delay)
        {
            Instance.Schedule(command, delay);
        }

        #endregion
    }
}