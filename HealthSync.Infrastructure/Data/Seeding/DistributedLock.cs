using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HealthSync.Infrastructure.Data.Seeding;

/// <summary>
/// Distributed lock implementation using SQL Server's sp_getapplock.
/// Ensures only one instance can acquire the lock at a time across multiple servers.
/// </summary>
public sealed class DistributedLock : IAsyncDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger _logger;
    private readonly string _lockName;
    private bool _lockAcquired;

    private DistributedLock(ApplicationDbContext context, ILogger logger, string lockName)
    {
        _context = context;
        _logger = logger;
        _lockName = lockName;
        _lockAcquired = false;
    }

    /// <summary>
    /// Tries to acquire a distributed lock with the specified name.
    /// </summary>
    /// <param name="context">Database context</param>
    /// <param name="logger">Logger instance</param>
    /// <param name="lockName">Unique name for the lock (max 255 chars)</param>
    /// <param name="timeoutMs">Timeout in milliseconds to wait for lock. 0 = no wait, -1 = wait indefinitely</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>DistributedLock instance if acquired, null if failed to acquire</returns>
    public static async Task<DistributedLock?> TryAcquireAsync(
        ApplicationDbContext context,
        ILogger logger,
        string lockName,
        int timeoutMs = 0,
        CancellationToken cancellationToken = default)
    {
        var distributedLock = new DistributedLock(context, logger, lockName);

        try
        {
            // sp_getapplock returns:
            // 0 or 1: Lock acquired successfully
            // -1: Lock request timed out
            // -2: Lock request was canceled
            // -3: Lock request was a deadlock victim
            // -999: Parameter validation or other call error
            var result = await context.Database.ExecuteSqlRawAsync(
                "EXEC @result = sp_getapplock @Resource = {0}, @LockMode = 'Exclusive', @LockOwner = 'Session', @LockTimeout = {1}",
                cancellationToken,
                lockName,
                timeoutMs);

            // Check if lock was acquired (result >= 0 means success)
            // Note: ExecuteSqlRawAsync returns number of rows affected, not the sp return value
            // We need to use a different approach to check the result
            var lockResult = await context.Database
                .SqlQueryRaw<int>(
                    @"DECLARE @result int;
                      EXEC @result = sp_getapplock @Resource = {0}, @LockMode = 'Exclusive', @LockOwner = 'Session', @LockTimeout = {1};
                      SELECT @result AS Value",
                    lockName,
                    timeoutMs)
                .FirstOrDefaultAsync(cancellationToken);

            if (lockResult >= 0)
            {
                distributedLock._lockAcquired = true;
                logger.LogInformation("Acquired distributed lock '{LockName}'", lockName);
                return distributedLock;
            }

            logger.LogWarning(
                "Failed to acquire distributed lock '{LockName}'. Result: {Result}. Another instance may be seeding.",
                lockName, lockResult);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Exception while trying to acquire distributed lock '{LockName}'", lockName);
            return null;
        }
    }

    /// <summary>
    /// Releases the distributed lock.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_lockAcquired)
        {
            try
            {
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC sp_releaseapplock @Resource = {0}, @LockOwner = 'Session'",
                    _lockName);

                _logger.LogInformation("Released distributed lock '{LockName}'", _lockName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to release distributed lock '{LockName}'", _lockName);
            }
            finally
            {
                _lockAcquired = false;
            }
        }
    }
}
