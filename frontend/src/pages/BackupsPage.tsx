import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { sbcApi } from '../api/sbcApi';
import { EmptyState } from '../components/EmptyState';
import { StatusBadge } from '../components/StatusBadge';
import { SummaryCard } from '../components/SummaryCard';
import type { BackupJobSummary, LatestBackupSystem } from '../types/dashboard';
import { formatDateTime, formatReason } from '../utils/formatters';

type BackupStatusFilter =
  | 'all'
  | 'Success'
  | 'Failed'
  | 'PendingValidation'
  | 'NoBackupJob'
  | 'RemovedFromUrBackup';

export function BackupsPage() {
  const [systems, setSystems] = useState<LatestBackupSystem[]>([]);
  const [searchText, setSearchText] = useState('');
  const [statusFilter, setStatusFilter] = useState<BackupStatusFilter>('all');
  const [attentionOnly, setAttentionOnly] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  async function loadBackups() {
    try {
      setIsLoading(true);
      setErrorMessage(null);

      const response = await sbcApi.get<LatestBackupSystem[]>('/dashboard/latest-backups');

      setSystems(response.data);
    } catch (error) {
      console.error(error);
      setErrorMessage('Could not load latest backups.');
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    loadBackups();
  }, []);

  const filteredSystems = useMemo(() => {
    const normalizedSearchText = searchText.trim().toLowerCase();

    return systems.filter((system) => {
      const searchableValues = [
        system.hostname,
        system.ipAddress,
        system.operatingSystem,
        system.backupCapability,
        system.latestStatus,
        system.simulator?.name,
        system.latestFileBackup?.status,
        system.latestFileBackup?.backupType,
        system.latestFileBackup?.backupPath,
        system.latestFileBackup?.errorMessage,
        system.latestImageBackup?.status,
        system.latestImageBackup?.backupType,
        system.latestImageBackup?.backupPath,
        system.latestImageBackup?.errorMessage,
      ]
        .filter((value): value is string => Boolean(value))
        .map((value) => value.toLowerCase());

      const matchesSearch =
        normalizedSearchText.length === 0 ||
        searchableValues.some((value) => value.includes(normalizedSearchText));

      const matchesStatus =
        statusFilter === 'all' ||
        system.latestStatus === statusFilter;

      const matchesAttention =
        !attentionOnly || system.requiresAttention;

      return matchesSearch && matchesStatus && matchesAttention;
    });
  }, [systems, searchText, statusFilter, attentionOnly]);

  const totalSystems = systems.length;
  const successCount = systems.filter((system) => system.latestStatus === 'Success').length;
  const failedCount = systems.filter((system) => system.latestStatus === 'Failed').length;
  const pendingValidationCount = systems.filter(
    (system) => system.latestStatus === 'PendingValidation'
  ).length;
  const noBackupJobCount = systems.filter((system) => system.latestStatus === 'NoBackupJob').length;
  const attentionCount = systems.filter((system) => system.requiresAttention).length;

  return (
    <div className="page-shell">
      <header className="page-title-row">
        <div>
          <h2>Backups</h2>
          <p>Latest file and image backup status per protected system.</p>
        </div>

        <button
          className="primary-button"
          type="button"
          onClick={loadBackups}
          disabled={isLoading}
        >
          {isLoading ? 'Refreshing...' : 'Refresh'}
        </button>
      </header>

      {errorMessage && (
        <section className="error-card">
          {errorMessage}
        </section>
      )}

      <section className="summary-grid">
        <SummaryCard
          title="Systems"
          value={totalSystems}
          description="Protected systems reviewed"
        />

        <SummaryCard
          title="Successful"
          value={successCount}
          description="Latest backup successful"
        />

        <SummaryCard
          title="Failed"
          value={failedCount}
          description="Latest backup failed"
          variant={failedCount > 0 ? 'danger' : 'default'}
        />

        <SummaryCard
          title="Pending validation"
          value={pendingValidationCount}
          description="Manual or pending validation"
          variant={pendingValidationCount > 0 ? 'warning' : 'default'}
        />

        <SummaryCard
          title="No backup job"
          value={noBackupJobCount}
          description="No backup job registered"
          variant={noBackupJobCount > 0 ? 'warning' : 'default'}
        />

        <SummaryCard
          title="Requires attention"
          value={attentionCount}
          description="Systems needing review"
          variant={attentionCount > 0 ? 'warning' : 'default'}
        />
      </section>

      <section className="dashboard-section">
        <div className="section-header">
          <div>
            <h3>Latest backups</h3>
            <p>Most recent backup state grouped by system.</p>
          </div>

          <span className="section-count">
            {filteredSystems.length}
          </span>
        </div>

        <div className="filters-row filters-row--three">
          <input
            className="filter-input"
            type="search"
            placeholder="Search by hostname, OS, simulator, capability or status..."
            value={searchText}
            onChange={(event) => setSearchText(event.target.value)}
          />

          <select
            className="filter-select"
            value={statusFilter}
            onChange={(event) => setStatusFilter(event.target.value as BackupStatusFilter)}
          >
            <option value="all">All backup statuses</option>
            <option value="Success">Success</option>
            <option value="Failed">Failed</option>
            <option value="PendingValidation">Pending validation</option>
            <option value="NoBackupJob">No backup job</option>
            <option value="RemovedFromUrBackup">Removed from UrBackup</option>
          </select>

          <label className="filter-checkbox">
            <input
              type="checkbox"
              checked={attentionOnly}
              onChange={(event) => setAttentionOnly(event.target.checked)}
            />
            <span>Attention only</span>
          </label>
        </div>

        {isLoading && systems.length === 0 ? (
          <section className="status-card">
            Loading backups...
          </section>
        ) : (
          <BackupsTable systems={filteredSystems} />
        )}
      </section>
    </div>
  );
}

type BackupsTableProps = {
  systems: LatestBackupSystem[];
};

function BackupsTable({ systems }: BackupsTableProps) {
  if (systems.length === 0) {
    return (
      <EmptyState message="No backup records match the selected filters." />
    );
  }

  return (
    <div className="table-wrapper">
      <table className="data-table">
        <thead>
          <tr>
            <th>System</th>
            <th>Status</th>
            <th>Capability</th>
            <th>File backup</th>
            <th>Image backup</th>
            <th>Latest backup</th>
            <th>Attention</th>
            <th>Action</th>
          </tr>
        </thead>

        <tbody>
          {systems.map((system) => (
            <tr key={system.id}>
              <td>
                <strong>{system.hostname}</strong>
                <span>{system.operatingSystem ?? 'Unknown OS'}</span>

                {system.simulator && (
                  <span>{system.simulator.name}</span>
                )}

                {system.ipAddress && (
                  <span>{system.ipAddress}</span>
                )}
              </td>

              <td>
                <StatusBadge value={system.latestStatus} />
              </td>

              <td>
                <strong>{formatReason(system.backupCapability)}</strong>
                <span>
                  File: {system.fileBackupValidated ? 'validated' : 'not validated'}
                </span>
                <span>
                  Image: {system.imageBackupValidated ? 'validated' : 'not validated'}
                </span>
              </td>

              <td>
                <BackupJobCompact job={system.latestFileBackup} />
              </td>

              <td>
                <BackupJobCompact job={system.latestImageBackup} />
              </td>

              <td>
                <BackupJobCompact job={system.latestAnyBackup} />
              </td>

              <td>
                <span className={system.requiresAttention ? 'boolean-failed' : 'boolean-ok'}>
                  {system.requiresAttention ? 'Yes' : 'No'}
                </span>
              </td>

              <td>
                <Link
                  className="table-action-link"
                  to={`/systems/${system.id}`}
                >
                  Details
                </Link>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

type BackupJobCompactProps = {
  job: BackupJobSummary | null;
};

function BackupJobCompact({ job }: BackupJobCompactProps) {
  if (!job) {
    return (
      <span className="muted-text">
        No job
      </span>
    );
  }

  return (
    <div className="backup-job-compact">
      <strong>{formatReason(job.status)}</strong>
      <span>{formatReason(job.backupType)}</span>

      <span>
        {formatDateTime(job.finishedAtUtc ?? job.startedAtUtc)}
      </span>

      {job.durationSeconds !== null && job.durationSeconds !== undefined && (
        <span>
          Duration: {formatDuration(job.durationSeconds)}
        </span>
      )}

      {job.sizeBytes !== null && job.sizeBytes !== undefined && (
        <span>
          Size: {formatBytes(job.sizeBytes)}
        </span>
      )}

      {job.backupPath && (
        <span title={job.backupPath}>
          Path: {job.backupPath}
        </span>
      )}

      {job.urBackupJobId && (
        <span>
          Job id: {job.urBackupJobId}
        </span>
      )}

      {job.errorMessage && (
        <span className="error-text">
          {job.errorMessage}
        </span>
      )}
    </div>
  );
}

function formatDuration(seconds: number) {
  if (seconds < 60) {
    return `${seconds}s`;
  }

  const minutes = Math.floor(seconds / 60);
  const remainingSeconds = seconds % 60;

  if (minutes < 60) {
    return `${minutes}m ${remainingSeconds}s`;
  }

  const hours = Math.floor(minutes / 60);
  const remainingMinutes = minutes % 60;

  return `${hours}h ${remainingMinutes}m`;
}

function formatBytes(bytes: number) {
  if (bytes <= 0) {
    return '0 B';
  }

  const units = ['B', 'KB', 'MB', 'GB', 'TB'];
  const unitIndex = Math.min(
    Math.floor(Math.log(bytes) / Math.log(1024)),
    units.length - 1
  );

  const value = bytes / Math.pow(1024, unitIndex);

  return `${value.toFixed(value >= 10 ? 0 : 1)} ${units[unitIndex]}`;
}