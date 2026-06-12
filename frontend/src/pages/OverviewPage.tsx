import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { sbcApi } from '../api/sbcApi';
import { BooleanStatus } from '../components/BooleanStatus';
import { EmptyState } from '../components/EmptyState';
import { StatusBadge } from '../components/StatusBadge';
import { SummaryCard } from '../components/SummaryCard';
import type { AttentionSystem, DashboardSummary } from '../types/dashboard';
import { formatDateTime, formatReason } from '../utils/formatters';

export function OverviewPage() {
  const [summary, setSummary] = useState<DashboardSummary | null>(null);
  const [attentionSystems, setAttentionSystems] = useState<AttentionSystem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  async function loadOverview() {
    try {
      setIsLoading(true);
      setErrorMessage(null);

      const [summaryResponse, attentionResponse] = await Promise.all([
        sbcApi.get<DashboardSummary>('/dashboard/summary'),
        sbcApi.get<AttentionSystem[]>('/dashboard/attention-systems'),
      ]);

      setSummary(summaryResponse.data);
      setAttentionSystems(attentionResponse.data);
    } catch (error) {
      console.error(error);
      setErrorMessage('Could not load overview data.');
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    loadOverview();
  }, []);

  return (
    <div className="page-shell">
      <header className="page-title-row">
        <div>
          <h2>Overview</h2>
          <p>General backup status and systems requiring attention.</p>
        </div>

        <button
          className="primary-button"
          type="button"
          onClick={loadOverview}
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

      {isLoading && !summary && (
        <section className="status-card">
          Loading overview...
        </section>
      )}

      {summary && (
        <>
          <section className="summary-grid">
            <SummaryCard
              title="Total systems"
              value={summary.totalSystems}
              description="Registered protected systems"
            />

            <SummaryCard
              title="UrBackup integrated"
              value={summary.urBackupIntegratedSystems}
              description="Systems linked with UrBackup"
            />

            <SummaryCard
              title="Manual / pending"
              value={summary.manualOrPendingSystems}
              description="Systems not fully integrated"
              variant={summary.manualOrPendingSystems > 0 ? 'warning' : 'default'}
            />

            <SummaryCard
              title="Online"
              value={summary.onlineSystems}
              description="Systems currently online"
            />

            <SummaryCard
              title="Offline"
              value={summary.offlineSystems}
              description="Systems currently offline"
              variant={summary.offlineSystems > 0 ? 'warning' : 'default'}
            />

            <SummaryCard
              title="Backup OK"
              value={summary.backupOkSystems}
              description="Systems with valid backups"
            />

            <SummaryCard
              title="With issues"
              value={summary.backupWithIssuesSystems}
              description="Systems reporting backup issues"
              variant={summary.backupWithIssuesSystems > 0 ? 'warning' : 'default'}
            />

            <SummaryCard
              title="No successful backup"
              value={summary.noSuccessfulBackupSystems}
              description="Systems without a successful backup"
              variant={summary.noSuccessfulBackupSystems > 0 ? 'danger' : 'default'}
            />

            <SummaryCard
              title="Open alerts"
              value={summary.openAlerts}
              description="Active alerts"
              variant={summary.openAlerts > 0 ? 'danger' : 'default'}
            />

            <SummaryCard
              title="Removed"
              value={summary.removedFromUrBackupSystems}
              description="Missing from UrBackup"
              variant={summary.removedFromUrBackupSystems > 0 ? 'danger' : 'default'}
            />

            <SummaryCard
              title="Last sync"
              value={formatDateTime(summary.lastUrBackupSyncAtUtc)}
              description="Last UrBackup synchronization"
            />
          </section>

          <section className="dashboard-section">
            <div className="section-header">
              <div>
                <h3>Systems requiring attention</h3>
                <p>Systems with backup issues, missing integration or no successful backup.</p>
              </div>

              <div className="section-actions">
                <span className="section-count">
                  {attentionSystems.length}
                </span>

                <Link className="secondary-link" to="/systems">
                  View all systems
                </Link>
              </div>
            </div>

            <AttentionSystemsTable systems={attentionSystems} />
          </section>
        </>
      )}
    </div>
  );
}

type AttentionSystemsTableProps = {
  systems: AttentionSystem[];
};

function AttentionSystemsTable({ systems }: AttentionSystemsTableProps) {
  if (systems.length === 0) {
    return (
      <EmptyState message="No systems require attention." />
    );
  }

  return (
    <div className="table-wrapper">
      <table className="data-table">
        <thead>
          <tr>
            <th>System</th>
            <th>Severity</th>
            <th>Reason</th>
            <th>Operational</th>
            <th>File backup</th>
            <th>Image backup</th>
            <th>Issues</th>
            <th>Last seen</th>
            <th>Action</th>
          </tr>
        </thead>

        <tbody>
          {systems.map((system) => (
            <tr key={system.id}>
              <td>
                <strong>{system.hostname}</strong>
                <span>{system.operatingSystem ?? 'Unknown OS'}</span>

                {system.ipAddress && (
                  <span>{system.ipAddress}</span>
                )}

                {system.simulator && (
                  <span>{system.simulator.name}</span>
                )}
              </td>

              <td>
                <StatusBadge value={system.severity} />
              </td>

              <td>
                <strong>{formatReason(system.reason)}</strong>
                <span>{system.description}</span>
              </td>

              <td>
                <SystemOperationalStatus system={system} />
              </td>

              <td>
                <BooleanStatus value={system.lastFileBackupOk} />
                <span>{formatDateTime(system.lastFileBackupAtUtc)}</span>
              </td>

              <td>
                <BooleanStatus value={system.lastImageBackupOk} />
                <span>{formatDateTime(system.lastImageBackupAtUtc)}</span>
              </td>

              <td>
                {system.lastFileBackupIssues ?? '-'}
              </td>

              <td>
                {formatDateTime(system.lastSeenAtUtc)}
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

type SystemOperationalStatusProps = {
  system: AttentionSystem;
};

function SystemOperationalStatus({ system }: SystemOperationalStatusProps) {
  if (system.isRemovedFromUrBackup) {
    return (
      <StatusBadge value="RemovedFromUrBackup" />
    );
  }

  if (system.isOnline) {
    return (
      <StatusBadge value="Online" />
    );
  }

  return (
    <StatusBadge value="Offline" />
  );
}