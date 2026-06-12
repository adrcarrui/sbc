import { useEffect, useMemo, useState } from 'react';
import { sbcApi } from '../api/sbcApi';
import { BooleanStatus } from '../components/BooleanStatus';
import { DetailItem } from '../components/DetailItem';
import { EmptyState } from '../components/EmptyState';
import { StatusBadge } from '../components/StatusBadge';
import { SummaryCard } from '../components/SummaryCard';
import type {
  UrBackupClientSyncResult,
  UrBackupHealthResult,
  UrBackupRawClientStatus,
  UrBackupRawStatusResult,
} from '../types/dashboard';
import { formatDateTime } from '../utils/formatters';

export function UrBackupPage() {
  const [health, setHealth] = useState<UrBackupHealthResult | null>(null);
  const [rawStatus, setRawStatus] = useState<UrBackupRawStatusResult | null>(null);
  const [syncResult, setSyncResult] = useState<UrBackupClientSyncResult | null>(null);

  const [isLoading, setIsLoading] = useState(true);
  const [isSyncing, setIsSyncing] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  async function loadUrBackupStatus() {
    try {
      setIsLoading(true);
      setErrorMessage(null);

      const [healthResponse, rawStatusResponse] = await Promise.all([
        sbcApi.get<UrBackupHealthResult>('/urbackup/status'),
        sbcApi.get<UrBackupRawStatusResult>('/urbackup/raw-status'),
      ]);

      setHealth(healthResponse.data);
      setRawStatus(rawStatusResponse.data);
    } catch (error) {
      console.error(error);
      setErrorMessage('Could not load UrBackup status.');
    } finally {
      setIsLoading(false);
    }
  }

  async function syncClients() {
    try {
      setIsSyncing(true);
      setErrorMessage(null);

      const response = await sbcApi.post<UrBackupClientSyncResult>('/urbackup/sync-clients');

      setSyncResult(response.data);

      await loadUrBackupStatus();
    } catch (error) {
      console.error(error);
      setErrorMessage('Could not synchronize UrBackup clients.');
    } finally {
      setIsSyncing(false);
    }
  }

  useEffect(() => {
    loadUrBackupStatus();
  }, []);

  const rawClients = useMemo(
    () => parseRawClients(rawStatus?.rawJson),
    [rawStatus]
  );

  const onlineClients = rawClients.filter((client) => client.online).length;
  const offlineClients = rawClients.filter((client) => !client.online).length;
  const fileBackupOkClients = rawClients.filter((client) => client.file_ok).length;
  const imageBackupOkClients = rawClients.filter((client) => client.image_ok).length;
  const clientsWithIssues = rawClients.filter(
    (client) => (client.last_filebackup_issues ?? 0) > 0
  ).length;

  return (
    <div className="page-shell">
      <header className="page-title-row">
        <div>
          <h2>UrBackup</h2>
          <p>UrBackup server health, raw status and manual synchronization tools.</p>
        </div>

        <div className="header-button-group">
          <button
            className="secondary-button"
            type="button"
            onClick={loadUrBackupStatus}
            disabled={isLoading || isSyncing}
          >
            {isLoading ? 'Refreshing...' : 'Refresh'}
          </button>

          <button
            className="primary-button"
            type="button"
            onClick={syncClients}
            disabled={isLoading || isSyncing}
          >
            {isSyncing ? 'Syncing...' : 'Sync clients'}
          </button>
        </div>
      </header>

      {errorMessage && (
        <section className="error-card">
          {errorMessage}
        </section>
      )}

      {isLoading && !health && (
        <section className="status-card">
          Loading UrBackup status...
        </section>
      )}

      {health && (
        <section className="summary-grid">
          <SummaryCard
            title="Server status"
            value={health.isReachable ? 'Reachable' : 'Unreachable'}
            description="UrBackup API availability"
            variant={health.isReachable ? 'default' : 'danger'}
          />

          <SummaryCard
            title="HTTP status"
            value={health.statusCode?.toString() ?? 'N/A'}
            description="Last HTTP response"
            variant={health.isReachable ? 'default' : 'danger'}
          />

          <SummaryCard
            title="Clients"
            value={rawClients.length}
            description="Detected UrBackup clients"
          />

          <SummaryCard
            title="Checked at"
            value={formatDateTime(health.checkedAtUtc)}
            description="Last server health check"
          />

          <SummaryCard
            title="Online"
            value={onlineClients}
            description="Clients currently online"
          />

          <SummaryCard
            title="Offline"
            value={offlineClients}
            description="Clients currently offline"
            variant={offlineClients > 0 ? 'warning' : 'default'}
          />

          <SummaryCard
            title="File backup OK"
            value={fileBackupOkClients}
            description="Clients with valid file backup"
          />

          <SummaryCard
            title="Image backup OK"
            value={imageBackupOkClients}
            description="Clients with valid image backup"
          />

          <SummaryCard
            title="With issues"
            value={clientsWithIssues}
            description="Clients reporting file backup issues"
            variant={clientsWithIssues > 0 ? 'warning' : 'default'}
          />
        </section>
      )}

      {health?.errorMessage && (
        <section className="error-card">
          {health.errorMessage}
        </section>
      )}

      {syncResult && (
        <section className="dashboard-section">
          <div className="section-header">
            <div>
              <h3>Last manual sync result</h3>
              <p>{syncResult.message ?? 'Synchronization finished.'}</p>
            </div>

            <StatusBadge value={syncResult.success ? 'Success' : 'Failed'} />
          </div>

          <div className="sync-result-grid">
            <SyncMetric label="Discovered" value={syncResult.discoveredClients} />
            <SyncMetric label="Created" value={syncResult.createdClients} />
            <SyncMetric label="Updated" value={syncResult.updatedClients} />
            <SyncMetric label="Restored" value={syncResult.restoredClients} />
            <SyncMetric label="Removed" value={syncResult.removedClients} />
            <SyncMetric label="Skipped" value={syncResult.skippedClients} />
          </div>

          {syncResult.errorMessage && (
            <div className="inline-error">
              {syncResult.errorMessage}
            </div>
          )}

          <SyncedClientsTable clients={syncResult.syncedClients} />
        </section>
      )}

      <section className="dashboard-section">
        <div className="section-header">
          <div>
            <h3>Detected UrBackup clients</h3>
            <p>Clients currently returned by the UrBackup server status endpoint.</p>
          </div>

          <span className="section-count">
            {rawClients.length}
          </span>
        </div>

        <UrBackupClientsTable clients={rawClients} />
      </section>

      <section className="dashboard-section">
        <div className="section-header">
          <div>
            <h3>Raw status information</h3>
            <p>Technical status returned by UrBackup. Useful for diagnostics.</p>
          </div>
        </div>

        <div className="detail-grid">
          <DetailItem label="Base URL" value={health?.baseUrl ?? 'Not available'} />
          <DetailItem label="API URL" value={rawStatus?.apiUrl ?? 'Not available'} />
          <DetailItem label="Health checked at" value={formatDateTime(health?.checkedAtUtc)} />
          <DetailItem label="Raw status checked at" value={formatDateTime(rawStatus?.checkedAtUtc)} />
          <DetailItem label="Health reachable" value={health?.isReachable ? 'Yes' : 'No'} />
          <DetailItem label="Raw status success" value={rawStatus?.success ? 'Yes' : 'No'} />
          <DetailItem label="HTTP status" value={health?.statusCode ?? 'Not available'} />
          <DetailItem label="Raw status error" value={rawStatus?.errorMessage ?? 'No error'} />
        </div>
      </section>
    </div>
  );
}

type SyncMetricProps = {
  label: string;
  value: number;
};

function SyncMetric({ label, value }: SyncMetricProps) {
  return (
    <div className="sync-metric">
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  );
}

type UrBackupClientsTableProps = {
  clients: UrBackupRawClientStatus[];
};

function UrBackupClientsTable({ clients }: UrBackupClientsTableProps) {
  if (clients.length === 0) {
    return (
      <EmptyState message="No clients returned by UrBackup." />
    );
  }

  return (
    <div className="table-wrapper">
      <table className="data-table">
        <thead>
          <tr>
            <th>Client</th>
            <th>Online</th>
            <th>File backup</th>
            <th>Image backup</th>
            <th>Issues</th>
            <th>Last seen</th>
            <th>Version</th>
          </tr>
        </thead>

        <tbody>
          {clients.map((client) => (
            <tr key={client.id}>
              <td>
                <strong>{client.name}</strong>
                <span>{client.os_version_string ?? client.os_simple ?? 'Unknown OS'}</span>

                {client.ip && (
                  <span>{client.ip}</span>
                )}
              </td>

              <td>
                <BooleanStatus
                  value={client.online}
                  trueLabel="Online"
                  falseLabel="Offline"
                />
              </td>

              <td>
                <BooleanStatus
                  value={client.file_ok}
                  trueLabel="OK"
                  falseLabel="Failed"
                />
                <span>{formatUnixDateTime(client.lastbackup)}</span>
              </td>

              <td>
                <BooleanStatus
                  value={client.image_ok}
                  trueLabel="OK"
                  falseLabel="Failed"
                />
                <span>{formatUnixDateTime(client.lastbackup_image)}</span>
              </td>

              <td>
                {client.last_filebackup_issues ?? 0}
              </td>

              <td>
                {formatUnixDateTime(client.lastseen)}
              </td>

              <td>
                {client.client_version_string ?? 'Not available'}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

type SyncedClientsTableProps = {
  clients: UrBackupClientSyncResult['syncedClients'];
};

function SyncedClientsTable({ clients }: SyncedClientsTableProps) {
  if (clients.length === 0) {
    return null;
  }

  return (
    <div className="sync-clients-block">
      <h4>Synchronized clients</h4>

      <div className="table-wrapper">
        <table className="data-table">
          <thead>
            <tr>
              <th>Client</th>
              <th>Action</th>
              <th>Online</th>
              <th>File backup</th>
              <th>Image backup</th>
              <th>Last sync</th>
            </tr>
          </thead>

          <tbody>
            {clients.map((client) => (
              <tr key={`${client.urBackupClientId ?? client.name}-${client.action}`}>
                <td>
                  <strong>{client.name}</strong>
                  <span>{client.operatingSystem ?? 'Unknown OS'}</span>
                </td>

                <td>
                  <StatusBadge value={client.action} />
                </td>

                <td>
                  <BooleanStatus
                    value={client.online}
                    trueLabel="Online"
                    falseLabel="Offline"
                  />
                </td>

                <td>
                  <BooleanStatus
                    value={client.fileBackupOk}
                    trueLabel="OK"
                    falseLabel="Failed"
                  />
                  <span>{formatDateTime(client.lastFileBackupAtUtc)}</span>
                </td>

                <td>
                  <BooleanStatus
                    value={client.imageBackupOk}
                    trueLabel="OK"
                    falseLabel="Failed"
                  />
                  <span>{formatDateTime(client.lastImageBackupAtUtc)}</span>
                </td>

                <td>
                  {formatDateTime(client.lastUrBackupSyncAtUtc)}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

function parseRawClients(rawJson: string | null | undefined): UrBackupRawClientStatus[] {
  if (!rawJson) {
    return [];
  }

  try {
    const parsed = JSON.parse(rawJson) as { status?: UrBackupRawClientStatus[] };

    return parsed.status ?? [];
  } catch (error) {
    console.error('Could not parse UrBackup raw status:', error);
    return [];
  }
}

function formatUnixDateTime(value: number | null | undefined) {
  if (!value || value <= 0) {
    return 'Not available';
  }

  return formatDateTime(new Date(value * 1000).toISOString());
}