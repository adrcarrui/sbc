import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { sbcApi } from '../api/sbcApi';
import { BooleanStatus } from '../components/BooleanStatus';
import { EmptyState } from '../components/EmptyState';
import { StatusBadge } from '../components/StatusBadge';
import { SummaryCard } from '../components/SummaryCard';
import type { UrBackupSystem } from '../types/dashboard';
import { formatDateTime, formatReason } from '../utils/formatters';

export function SystemsPage() {
  const [systems, setSystems] = useState<UrBackupSystem[]>([]);
  const [searchText, setSearchText] = useState('');
  const [statusFilter, setStatusFilter] = useState('all');
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  async function loadSystems() {
    try {
      setIsLoading(true);
      setErrorMessage(null);

      const response = await sbcApi.get<UrBackupSystem[]>('/dashboard/urbackup-systems');

      setSystems(response.data);
    } catch (error) {
      console.error(error);
      setErrorMessage('Could not load protected systems.');
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    loadSystems();
  }, []);

  const filteredSystems = useMemo(() => {
    const normalizedSearchText = searchText.trim().toLowerCase();

    return systems.filter((system) => {
      const searchableValues = [
        system.hostname,
        system.operatingSystem,
        system.ipAddress,
        system.urBackupClientId,
        system.urBackupClientName,
        system.urBackupClientVersion,
        system.operationalStatus,
        system.backupStatus,
        system.simulator?.name,
      ]
        .filter((value): value is string => Boolean(value))
        .map((value) => value.toLowerCase());

      const matchesSearch =
        normalizedSearchText.length === 0 ||
        searchableValues.some((value) => value.includes(normalizedSearchText));

      const matchesStatus =
        statusFilter === 'all' ||
        system.operationalStatus === statusFilter ||
        system.backupStatus === statusFilter;

      return matchesSearch && matchesStatus;
    });
  }, [systems, searchText, statusFilter]);

  const totalSystems = systems.length;
  const integratedSystems = systems.filter((system) => isIntegratedWithUrBackup(system)).length;
  const onlineSystems = systems.filter((system) => system.isOnline).length;
  const offlineSystems = systems.filter((system) => !system.isOnline).length;
  const withIssues = systems.filter((system) => system.backupStatus === 'WithIssues').length;
  const removedSystems = systems.filter((system) => system.isRemovedFromUrBackup).length;

  return (
    <div className="page-shell">
      <header className="page-title-row">
        <div>
          <h2>Systems</h2>
          <p>Protected systems registered in SBC and synchronized with UrBackup.</p>
        </div>

        <button
          className="primary-button"
          type="button"
          onClick={loadSystems}
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
          title="Total systems"
          value={totalSystems}
          description="Registered protected systems"
        />

        <SummaryCard
          title="Integrated"
          value={integratedSystems}
          description="Linked with UrBackup"
        />

        <SummaryCard
          title="Online"
          value={onlineSystems}
          description="Currently online"
        />

        <SummaryCard
          title="Offline"
          value={offlineSystems}
          description="Currently offline"
          variant={offlineSystems > 0 ? 'warning' : 'default'}
        />

        <SummaryCard
          title="With issues"
          value={withIssues}
          description="Systems reporting backup issues"
          variant={withIssues > 0 ? 'warning' : 'default'}
        />

        <SummaryCard
          title="Removed"
          value={removedSystems}
          description="Missing from UrBackup"
          variant={removedSystems > 0 ? 'danger' : 'default'}
        />
      </section>

      <section className="dashboard-section">
        <div className="section-header">
          <div>
            <h3>Protected systems</h3>
            <p>Filter and review backup status per system.</p>
          </div>

          <span className="section-count">
            {filteredSystems.length}
          </span>
        </div>

        <div className="filters-row">
          <input
            className="filter-input"
            type="search"
            placeholder="Search by hostname, OS, IP, simulator or UrBackup client..."
            value={searchText}
            onChange={(event) => setSearchText(event.target.value)}
          />

          <select
            className="filter-select"
            value={statusFilter}
            onChange={(event) => setStatusFilter(event.target.value)}
          >
            <option value="all">All statuses</option>
            <option value="Online">Online</option>
            <option value="Offline">Offline</option>
            <option value="RemovedFromUrBackup">Removed from UrBackup</option>
            <option value="Ok">Backup OK</option>
            <option value="WithIssues">Backup with issues</option>
            <option value="NoSuccessfulBackup">No successful backup</option>
          </select>
        </div>

        {isLoading && systems.length === 0 ? (
          <section className="status-card">
            Loading systems...
          </section>
        ) : (
          <SystemsTable systems={filteredSystems} />
        )}
      </section>
    </div>
  );
}

type SystemsTableProps = {
  systems: UrBackupSystem[];
};

function SystemsTable({ systems }: SystemsTableProps) {
  if (systems.length === 0) {
    return (
      <EmptyState message="No systems match the selected filters." />
    );
  }

  return (
    <div className="table-wrapper">
      <table className="data-table">
        <thead>
          <tr>
            <th>System</th>
            <th>Operational</th>
            <th>Backup</th>
            <th>UrBackup</th>
            <th>Last file backup</th>
            <th>Last image backup</th>
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

                {system.simulator && (
                  <span>{system.simulator.name}</span>
                )}
              </td>

              <td>
                <StatusBadge value={system.operationalStatus} />
              </td>

              <td>
                <StatusBadge value={system.backupStatus} />
              </td>

              <td>
                <strong>{isIntegratedWithUrBackup(system) ? 'Integrated' : 'Not linked'}</strong>
                <span>{system.urBackupClientName ?? 'No UrBackup client'}</span>

                {system.urBackupClientVersion && (
                  <span>Version: {system.urBackupClientVersion}</span>
                )}
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

function isIntegratedWithUrBackup(system: UrBackupSystem) {
  return Boolean(system.urBackupClientId || system.urBackupClientName);
}