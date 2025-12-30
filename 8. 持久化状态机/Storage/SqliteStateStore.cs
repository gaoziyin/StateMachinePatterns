using Microsoft.Data.Sqlite;

namespace PersistentStatePattern.Storage;

using Core;

/// <summary>
/// SQLite状态存储实现（生产级持久化）
/// </summary>
public class SqliteStateStore : IStateStore, IAsyncDisposable
{
    private readonly string _connectionString;
    private SqliteConnection? _connection;
    
    public SqliteStateStore(string databasePath = "statemachine.db")
    {
        _connectionString = $"Data Source={databasePath}";
    }
    
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _connection = new SqliteConnection(_connectionString);
        await _connection.OpenAsync(cancellationToken);
        
        Console.WriteLine($"  [SQLite] 连接数据库: {_connectionString}");
        
        // 创建表结构
        var createTablesSql = """
            -- 状态机实例表
            CREATE TABLE IF NOT EXISTS StateMachineInstances (
                InstanceId TEXT PRIMARY KEY,
                MachineType TEXT NOT NULL,
                CurrentState TEXT NOT NULL,
                ContextJson TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                Version INTEGER NOT NULL DEFAULT 1,
                IsCompleted INTEGER NOT NULL DEFAULT 0
            );
            
            -- 状态转换历史表
            CREATE TABLE IF NOT EXISTS StateTransitions (
                Id TEXT PRIMARY KEY,
                InstanceId TEXT NOT NULL,
                FromState TEXT NOT NULL,
                ToState TEXT NOT NULL,
                Trigger TEXT NOT NULL,
                Timestamp TEXT NOT NULL,
                Metadata TEXT,
                FOREIGN KEY (InstanceId) REFERENCES StateMachineInstances(InstanceId)
            );
            
            -- 检查点表
            CREATE TABLE IF NOT EXISTS Checkpoints (
                CheckpointId TEXT PRIMARY KEY,
                InstanceId TEXT NOT NULL,
                State TEXT NOT NULL,
                ContextJson TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                Description TEXT,
                FOREIGN KEY (InstanceId) REFERENCES StateMachineInstances(InstanceId)
            );
            
            -- 索引
            CREATE INDEX IF NOT EXISTS idx_instances_type ON StateMachineInstances(MachineType);
            CREATE INDEX IF NOT EXISTS idx_instances_state ON StateMachineInstances(CurrentState);
            CREATE INDEX IF NOT EXISTS idx_transitions_instance ON StateTransitions(InstanceId);
            CREATE INDEX IF NOT EXISTS idx_checkpoints_instance ON Checkpoints(InstanceId);
        """;
        
        using var command = new SqliteCommand(createTablesSql, _connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
        
        Console.WriteLine("  [SQLite] 数据库表结构已创建");
    }
    
    public async Task SaveInstanceAsync(StateMachineData data, CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        
        var sql = """
            INSERT INTO StateMachineInstances 
                (InstanceId, MachineType, CurrentState, ContextJson, CreatedAt, UpdatedAt, Version, IsCompleted)
            VALUES 
                (@InstanceId, @MachineType, @CurrentState, @ContextJson, @CreatedAt, @UpdatedAt, @Version, @IsCompleted)
            ON CONFLICT(InstanceId) DO UPDATE SET
                CurrentState = @CurrentState,
                ContextJson = @ContextJson,
                UpdatedAt = @UpdatedAt,
                Version = @Version,
                IsCompleted = @IsCompleted
        """;
        
        using var command = new SqliteCommand(sql, _connection);
        command.Parameters.AddWithValue("@InstanceId", data.InstanceId.ToString());
        command.Parameters.AddWithValue("@MachineType", data.MachineType);
        command.Parameters.AddWithValue("@CurrentState", data.CurrentState);
        command.Parameters.AddWithValue("@ContextJson", data.ContextJson);
        command.Parameters.AddWithValue("@CreatedAt", data.CreatedAt.ToString("O"));
        command.Parameters.AddWithValue("@UpdatedAt", data.UpdatedAt.ToString("O"));
        command.Parameters.AddWithValue("@Version", data.Version);
        command.Parameters.AddWithValue("@IsCompleted", data.IsCompleted ? 1 : 0);
        
        await command.ExecuteNonQueryAsync(cancellationToken);
        Console.WriteLine($"  [SQLite] 保存实例: {data.InstanceId:N} 状态: {data.CurrentState}");
    }
    
    public async Task<StateMachineData?> LoadInstanceAsync(Guid instanceId, CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        
        var sql = "SELECT * FROM StateMachineInstances WHERE InstanceId = @InstanceId";
        
        using var command = new SqliteCommand(sql, _connection);
        command.Parameters.AddWithValue("@InstanceId", instanceId.ToString());
        
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            var data = ReadInstanceFromReader(reader);
            Console.WriteLine($"  [SQLite] 加载实例: {instanceId:N}");
            return data;
        }
        
        return null;
    }
    
    public async Task DeleteInstanceAsync(Guid instanceId, CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        
        var sql = """
            DELETE FROM Checkpoints WHERE InstanceId = @InstanceId;
            DELETE FROM StateTransitions WHERE InstanceId = @InstanceId;
            DELETE FROM StateMachineInstances WHERE InstanceId = @InstanceId;
        """;
        
        using var command = new SqliteCommand(sql, _connection);
        command.Parameters.AddWithValue("@InstanceId", instanceId.ToString());
        await command.ExecuteNonQueryAsync(cancellationToken);
        
        Console.WriteLine($"  [SQLite] 删除实例: {instanceId:N}");
    }
    
    public async Task<IReadOnlyList<StateMachineData>> QueryInstancesAsync(
        string? machineType = null,
        string? currentState = null,
        bool? isCompleted = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        
        var sql = "SELECT * FROM StateMachineInstances WHERE 1=1";
        var parameters = new List<SqliteParameter>();
        
        if (machineType != null)
        {
            sql += " AND MachineType = @MachineType";
            parameters.Add(new SqliteParameter("@MachineType", machineType));
        }
        if (currentState != null)
        {
            sql += " AND CurrentState = @CurrentState";
            parameters.Add(new SqliteParameter("@CurrentState", currentState));
        }
        if (isCompleted.HasValue)
        {
            sql += " AND IsCompleted = @IsCompleted";
            parameters.Add(new SqliteParameter("@IsCompleted", isCompleted.Value ? 1 : 0));
        }
        
        sql += " ORDER BY CreatedAt DESC LIMIT @Take OFFSET @Skip";
        parameters.Add(new SqliteParameter("@Take", take));
        parameters.Add(new SqliteParameter("@Skip", skip));
        
        using var command = new SqliteCommand(sql, _connection);
        command.Parameters.AddRange(parameters.ToArray());
        
        var results = new List<StateMachineData>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(ReadInstanceFromReader(reader));
        }
        
        return results;
    }
    
    public async Task RecordTransitionAsync(StateTransitionRecord record, CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        
        var sql = """
            INSERT INTO StateTransitions 
                (Id, InstanceId, FromState, ToState, Trigger, Timestamp, Metadata)
            VALUES 
                (@Id, @InstanceId, @FromState, @ToState, @Trigger, @Timestamp, @Metadata)
        """;
        
        using var command = new SqliteCommand(sql, _connection);
        command.Parameters.AddWithValue("@Id", record.Id.ToString());
        command.Parameters.AddWithValue("@InstanceId", record.InstanceId.ToString());
        command.Parameters.AddWithValue("@FromState", record.FromState);
        command.Parameters.AddWithValue("@ToState", record.ToState);
        command.Parameters.AddWithValue("@Trigger", record.Trigger);
        command.Parameters.AddWithValue("@Timestamp", record.Timestamp.ToString("O"));
        command.Parameters.AddWithValue("@Metadata", record.Metadata ?? (object)DBNull.Value);
        
        await command.ExecuteNonQueryAsync(cancellationToken);
        Console.WriteLine($"  [SQLite] 记录转换: {record.FromState} → {record.ToState}");
    }
    
    public async Task<IReadOnlyList<StateTransitionRecord>> GetTransitionHistoryAsync(
        Guid instanceId, 
        CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        
        var sql = "SELECT * FROM StateTransitions WHERE InstanceId = @InstanceId ORDER BY Timestamp";
        
        using var command = new SqliteCommand(sql, _connection);
        command.Parameters.AddWithValue("@InstanceId", instanceId.ToString());
        
        var results = new List<StateTransitionRecord>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new StateTransitionRecord(
                Guid.Parse(reader.GetString(0)),
                Guid.Parse(reader.GetString(1)),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                DateTime.Parse(reader.GetString(5)),
                reader.IsDBNull(6) ? null : reader.GetString(6)
            ));
        }
        
        return results;
    }
    
    public async Task CreateCheckpointAsync(StateMachineCheckpoint checkpoint, CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        
        var sql = """
            INSERT INTO Checkpoints 
                (CheckpointId, InstanceId, State, ContextJson, CreatedAt, Description)
            VALUES 
                (@CheckpointId, @InstanceId, @State, @ContextJson, @CreatedAt, @Description)
        """;
        
        using var command = new SqliteCommand(sql, _connection);
        command.Parameters.AddWithValue("@CheckpointId", checkpoint.CheckpointId.ToString());
        command.Parameters.AddWithValue("@InstanceId", checkpoint.InstanceId.ToString());
        command.Parameters.AddWithValue("@State", checkpoint.State);
        command.Parameters.AddWithValue("@ContextJson", checkpoint.ContextJson);
        command.Parameters.AddWithValue("@CreatedAt", checkpoint.CreatedAt.ToString("O"));
        command.Parameters.AddWithValue("@Description", checkpoint.Description ?? (object)DBNull.Value);
        
        await command.ExecuteNonQueryAsync(cancellationToken);
        Console.WriteLine($"  [SQLite] 创建检查点: {checkpoint.CheckpointId:N}");
    }
    
    public async Task<IReadOnlyList<StateMachineCheckpoint>> GetCheckpointsAsync(
        Guid instanceId, 
        CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        
        var sql = "SELECT * FROM Checkpoints WHERE InstanceId = @InstanceId ORDER BY CreatedAt";
        
        using var command = new SqliteCommand(sql, _connection);
        command.Parameters.AddWithValue("@InstanceId", instanceId.ToString());
        
        var results = new List<StateMachineCheckpoint>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new StateMachineCheckpoint(
                Guid.Parse(reader.GetString(0)),
                Guid.Parse(reader.GetString(1)),
                reader.GetString(2),
                reader.GetString(3),
                DateTime.Parse(reader.GetString(4)),
                reader.IsDBNull(5) ? null : reader.GetString(5)
            ));
        }
        
        return results;
    }
    
    public async Task<StateMachineCheckpoint?> GetCheckpointAsync(
        Guid checkpointId, 
        CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        
        var sql = "SELECT * FROM Checkpoints WHERE CheckpointId = @CheckpointId";
        
        using var command = new SqliteCommand(sql, _connection);
        command.Parameters.AddWithValue("@CheckpointId", checkpointId.ToString());
        
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return new StateMachineCheckpoint(
                Guid.Parse(reader.GetString(0)),
                Guid.Parse(reader.GetString(1)),
                reader.GetString(2),
                reader.GetString(3),
                DateTime.Parse(reader.GetString(4)),
                reader.IsDBNull(5) ? null : reader.GetString(5)
            );
        }
        
        return null;
    }
    
    private void EnsureConnected()
    {
        if (_connection == null)
            throw new InvalidOperationException("数据库未初始化，请先调用 InitializeAsync()");
    }
    
    private static StateMachineData ReadInstanceFromReader(SqliteDataReader reader)
    {
        return new StateMachineData
        {
            InstanceId = Guid.Parse(reader.GetString(0)),
            MachineType = reader.GetString(1),
            CurrentState = reader.GetString(2),
            ContextJson = reader.GetString(3),
            CreatedAt = DateTime.Parse(reader.GetString(4)),
            UpdatedAt = DateTime.Parse(reader.GetString(5)),
            Version = reader.GetInt64(6),
            IsCompleted = reader.GetInt32(7) == 1
        };
    }
    
    public async ValueTask DisposeAsync()
    {
        if (_connection != null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }
}
