import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { sbcApi } from '../api/sbcApi';
import { DetailItem } from '../components/DetailItem';
import { EmptyState } from '../components/EmptyState';
import { StatusBadge } from '../components/StatusBadge';
import { SummaryCard } from '../components/SummaryCard';
import type { UrBackupSystemDetail } from '../types/dashboard';
import { formatDateTime, formatReason } from '../utils/formatters';

export function SystemDetailPage() {
  const { id } = useParams<{ id: string }>();

  const [detail, setDetail] = useState<UrBackupSystemDetail | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  async function loadDetail() {
    if (!id) {
      setErrorMessage('System id is missing.');
      setIsLoading(false);
      return;
    }

    try {
      setIsLoading(true);
      setErrorMessage(null);

      const response = await sbcApi.get<UrBackupSystemDetail>(
        `/protected-systems/${id}/urbackup-detail`
      );

      setDetail(response.data);
    } catch (error) {
      console.error(error);
      setErrorMessage('Could not load system detail.');
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    loadDetail();
  }, [id]);

  return (
    <div className="page-shell">
      <header className="page-title-row">
        <div>
          <Link className="secondary-link" to="/systems">
            ← Back to systems
          </Link>

          <h2>{detail?.hostname ?? 'System detail'}</h2>
          <p>{detail?.operatingSystem ?? 'Protected system backup detail.'}</p>
        </div>

        <button
          className="primary-button"
          type="button"
          onClick={loadDetail}
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

      {isLoading && !detail && (
        <section className="status-card">
          Loading system detail...
        </section>
      )}

      {detail && (
        <>
          <section className="summary-grid">
            <SummaryCard
              title="Operational status"
              value={formatReason(detail.urBackup.operationalStatus)}
              description="Current system state"
              variant={getStatusVariant(detail.urBackup.operationalStatus)}
            />

            <SummaryCard
              title="Backup status"
              value={formatReason(detail.urBackup.backupStatus)}
              description="Current backup state"
              variant={getStatusVariant(detail.urBackup.backupStatus)}
            />

            <SummaryCard
              title="Backup capability"
              value={formatReason(detail.backupCapability)}
              description="Configured backup coverage"
            />

            <SummaryCard
              title="Open alerts"
              value={detail.openAlerts.length}
              description="Active alerts linked to this system"
              variant={detail.openAlerts.length > 0 ? 'danger' : 'default'}
            />
          </section>

          <section className="dashboard-section">
            <div className="section-header">
              <div>
                <h3>System information</h3>
                <p>Inventory and simulator assignment data.</p>
              </div>
            </div>

            <div className="detail-grid">
              <DetailItem label="Hostname" value={detail.hostname} />
              <DetailItem label="IP address" value={detail.ipAddress ?? 'Not available'} />
              <DetailItem label="Operating system" value={detail.operatingSystem ?? 'Unknown'} />
              <DetailItem label="Criticality" value={formatReason(detail.criticality)} />
              <DetailItem label="Active" value={detail.isActive ? 'Yes' : 'No'} />
              <DetailItem label="Simulator" value={detail.simulator?.name ?? 'Not assigned'} />
              <DetailItem label="File system" value={detail.fileSystem ?? 'Not available'} />
              <DetailItem label="Partition scheme" value={detail.partitionScheme ?? 'Not available'} />
              <DetailItem label="Notes" value={detail.notes ?? 'No notes'} />
            </div>
          </section>

          <section className="dashboard-section">
            <div className="section-header">
              <div>
                <h3>UrBackup status</h3>
                <p>Last synchronized data returned by UrBackup.</p>
              </div>
            </div>

            <div className="detail-grid">
              <DetailItem
                label="Integrated"
                value={detail.urBackup.isIntegrated ? 'Yes' : 'No'}
              />

              <DetailItem
                label="UrBackup client id"
                value={detail.urBackup.urBackupClientId ?? 'Not linked'}
              />

              <DetailItem
                label="UrBackup client"
                value={detail.urBackup.urBackupClientName ?? 'Not linked'}
              />

              <DetailItem
                label="Client version"
                value={detail.urBackup.urBackupClientVersion ?? 'Not available'}
              />

              <DetailItem
                label="Online"
                value={detail.urBackup.isOnline ? 'Yes' : 'No'}
              />

              <DetailItem
                label="Removed from UrBackup"
                value={detail.urBackup.isRemovedFromUrBackup ? 'Yes' : 'No'}
              />

              <DetailItem
                label="Removed at"
                value={formatDateTime(detail.urBackup.removedFromUrBackupAtUtc)}
              />

              <DetailItem
                label="Last sync"
                value={formatDateTime(detail.urBackup.lastUrBackupSyncAtUtc)}
              />

              <DetailItem
                label="Last seen"
                value={formatDateTime(detail.urBackup.lastSeenAtUtc)}
              />

              <DetailItem
                label="UrBackup status code"
                value={detail.urBackup.urBackupStatusCode ?? 'Not available'}
              />
            </div>
          </section>

          <section className="dashboard-section">
            <div className="section-header">
              <div>
                <h3>Backup status</h3>
                <p>Latest file and image backup results.</p>
              </div>
            </div>

            <div className="detail-grid">
              <DetailItem
                label="Last file backup"
                value={formatDateTime(detail.urBackup.lastFileBackupAtUtc)}
              />

              <DetailItem
                label="File backup OK"
                value={detail.urBackup.lastFileBackupOk ? 'Yes' : 'No'}
              />

              <DetailItem
                label="Last image backup"
                value={formatDateTime(detail.urBackup.lastImageBackupAtUtc)}
              />

              <DetailItem
                label="Image backup OK"
                value={detail.urBackup.lastImageBackupOk ? 'Yes' : 'No'}
              />

              <DetailItem
                label="File backup issues"
                value={detail.urBackup.lastFileBackupIssues ?? 0}
              />

              <DetailItem
                label="Backup capability"
                value={formatReason(detail.backupCapability)}
              />
            </div>
          </section>

          <section className="dashboard-section">
            <div className="section-header">
              <div>
                <h3>Open alerts</h3>
                <p>Active alerts linked to this system.</p>
              </div>

              <span className="section-count">
                {detail.openAlerts.length}
              </span>
            </div>

            <AlertsList alerts={detail.openAlerts} />
          </section>

          <section className="dashboard-section">
            <div className="section-header">
              <div>
                <h3>Recent alerts</h3>
                <p>Latest alert history for this system.</p>
              </div>

              <span className="section-count">
                {detail.recentAlerts.length}
              </span>
            </div>

            <AlertsList alerts={detail.recentAlerts} emptyMessage="No recent alerts for this system." />
          </section>

          <section className="dashboard-section">
            <div className="section-header">
              <div>
                <h3>Recent events</h3>
                <p>Latest backup events for this system.</p>
              </div>

              <span className="section-count">
                {detail.recentEvents.length}
              </span>
            </div>

            <EventsList events={detail.recentEvents} />
          </section>
        </>
      )}
    </div>
  );
}

type AlertsListProps = {
  alerts: UrBackupSystemDetail['openAlerts'];
  emptyMessage?: string;
};

function AlertsList({
  alerts,
  emptyMessage = 'No open alerts for this system.',
}: AlertsListProps) {
  if (alerts.length === 0) {
    return (
      <EmptyState message={emptyMessage} />
    );
  }

  return (
    <div className="detail-list">
      {alerts.map((alert) => (
        <article className="detail-list-item" key={alert.id}>
          <div>
            <strong>{alert.title}</strong>
            <span>{alert.message}</span>
            <small>{formatDateTime(alert.createdAtUtc)}</small>

            {alert.resolvedAtUtc && (
              <small>Resolved: {formatDateTime(alert.resolvedAtUtc)}</small>
            )}
          </div>

          <div className="detail-list-badges">
            <StatusBadge value={alert.severity} />
            <StatusBadge value={alert.status} />
          </div>
        </article>
      ))}
    </div>
  );
}

type EventsListProps = {
  events: UrBackupSystemDetail['recentEvents'];
};

function EventsList({ events }: EventsListProps) {
  if (events.length === 0) {
    return (
      <EmptyState message="No recent events for this system." />
    );
  }

  return (
    <div className="detail-list">
      {events.map((event) => (
        <article className="detail-list-item" key={event.id}>
          <div>
            <strong>{formatReason(event.eventType)}</strong>
            <span>{event.message}</span>
            <small>{formatDateTime(event.createdAtUtc)}</small>
          </div>

          <StatusBadge value={event.severity} />
        </article>
      ))}
    </div>
  );
}

function getStatusVariant(value: string): 'default' | 'warning' | 'danger' {
  if (
    value === 'WithIssues' ||
    value === 'PendingValidation' ||
    value === 'NoSuccessfulBackup'
  ) {
    return 'warning';
  }

  if (
    value === 'RemovedFromUrBackup' ||
    value === 'Failed' ||
    value === 'Critical'
  ) {
    return 'danger';
  }

  return 'default';
}