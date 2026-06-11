import { useEffect, useState } from 'react';
import './App.css';
import { sbcApi } from './api/sbcApi';
import type { AttentionSystem, DashboardSummary } from './types/dashboard';

function App() {
  const [summary, setSummary] = useState<DashboardSummary | null>(null);
  const [attentionSystems, setAttentionSystems] = useState<AttentionSystem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  async function loadDashboard() {
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
      setErrorMessage('Could not load dashboard data.');
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    loadDashboard();
  }, []);

  return (
    <main className="app-shell">
      <header className="page-header">
        <div>
          <h1>Simulator Backup Control</h1>
          <p>Centralized UrBackup monitoring dashboard</p>
        </div>

        <div className="header-actions">
          <div className="sync-info">
            <span>Last sync</span>
            <strong>{formatDateTime(summary?.lastUrBackupSyncAtUtc)}</strong>
          </div>

          <button
            className="refresh-button"
            type="button"
            onClick={loadDashboard}
            disabled={isLoading}
          >
            {isLoading ? 'Refreshing...' : 'Refresh'}
          </button>
        </div>
      </header>

      {isLoading && (
        <section className="status-card">
          Loading dashboard data...
        </section>
      )}

      {errorMessage && (
        <section className="error-card">
          {errorMessage}
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
              description="Systems not linked with UrBackup"
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
              variant="warning"
            />

            <SummaryCard
              title="No successful backup"
              value={summary.noSuccessfulBackupSystems}
              description="Systems without a successful backup"
              variant="danger"
            />

            <SummaryCard
              title="Open alerts"
              value={summary.openAlerts}
              description="Active alerts requiring attention"
              variant={summary.openAlerts > 0 ? 'danger' : 'default'}
            />

            <SummaryCard
              title="Removed from UrBackup"
              value={summary.removedFromUrBackupSystems}
              description="Previously synced but missing now"
              variant={summary.removedFromUrBackupSystems > 0 ? 'danger' : 'default'}
            />
          </section>

          <section className="dashboard-section">
            <div className="section-header">
              <div>
                <h2>Systems requiring attention</h2>
                <p>Systems with backup issues, missing integration or no successful backup.</p>
              </div>

              <span className="section-count">
                {attentionSystems.length}
              </span>
            </div>

            <AttentionSystemsTable systems={attentionSystems} />
          </section>
        </>
      )}
    </main>
  );
}

type SummaryCardProps = {
  title: string;
  value: number;
  description: string;
  variant?: 'default' | 'warning' | 'danger';
};

function SummaryCard({
  title,
  value,
  description,
  variant = 'default',
}: SummaryCardProps) {
  return (
    <article className={`summary-card summary-card--${variant}`}>
      <span>{title}</span>
      <strong>{value}</strong>
      <p>{description}</p>
    </article>
  );
}

type AttentionSystemsTableProps = {
  systems: AttentionSystem[];
};

function AttentionSystemsTable({ systems }: AttentionSystemsTableProps) {
  if (systems.length === 0) {
    return (
      <div className="empty-state">
        No systems require attention.
      </div>
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
            <th>File backup</th>
            <th>Image backup</th>
            <th>Issues</th>
            <th>Last seen</th>
          </tr>
        </thead>

        <tbody>
          {systems.map((system) => (
            <tr key={system.id}>
              <td>
                <strong>{system.hostname}</strong>
                <span>{system.operatingSystem ?? 'Unknown OS'}</span>
              </td>

              <td>
                <StatusBadge value={system.severity} />
              </td>

              <td>
                <strong>{formatReason(system.reason)}</strong>
                <span>{system.description}</span>
              </td>

              <td>
                <BooleanStatus value={system.lastFileBackupOk} />
              </td>

              <td>
                <BooleanStatus value={system.lastImageBackupOk} />
              </td>

              <td>
                {system.lastFileBackupIssues ?? '-'}
              </td>

              <td>
                {formatDateTime(system.lastSeenAtUtc)}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

type StatusBadgeProps = {
  value: string;
};

function StatusBadge({ value }: StatusBadgeProps) {
  const normalizedValue = value.toLowerCase();

  return (
    <span className={`status-badge status-badge--${normalizedValue}`}>
      {value}
    </span>
  );
}

type BooleanStatusProps = {
  value: boolean;
};

function BooleanStatus({ value }: BooleanStatusProps) {
  return (
    <span className={value ? 'boolean-ok' : 'boolean-failed'}>
      {value ? 'OK' : 'Failed'}
    </span>
  );
}

function formatDateTime(value: string | null | undefined) {
  if (!value) {
    return 'Not available';
  }

  return new Intl.DateTimeFormat('es-ES', {
    dateStyle: 'short',
    timeStyle: 'medium',
  }).format(new Date(value));
}

function formatReason(value: string) {
  return value
    .replace(/([A-Z])/g, ' $1')
    .replace(/^./, (firstLetter) => firstLetter.toUpperCase())
    .trim();
}

export default App;