import { useEffect, useMemo, useState } from 'react';
import type { SyntheticEvent } from 'react';
import { Link } from 'react-router-dom';
import { sbcApi } from '../api/sbcApi';
import { DetailItem } from '../components/DetailItem';
import { EmptyState } from '../components/EmptyState';
import { StatusBadge } from '../components/StatusBadge';
import { SummaryCard } from '../components/SummaryCard';
import type {
  CreateManualBackupRequestPayload,
  ManualBackupRequest,
  UrBackupSystem,
} from '../types/dashboard';
import { formatDateTime, formatReason } from '../utils/formatters';

type ManualRequestFilter =
  | 'all'
  | 'Pending'
  | 'InProgress'
  | 'Completed'
  | 'Validated'
  | 'Cancelled';

const initialCreateForm: CreateManualBackupRequestPayload = {
  protectedSystemId: '',
  requestedBy: '',
  assignedTo: '',
  reason: '',
  relatedChangeReference: '',
};

export function ManualRequestsPage() {
  const [requests, setRequests] = useState<ManualBackupRequest[]>([]);
  const [systems, setSystems] = useState<UrBackupSystem[]>([]);

  const [filter, setFilter] = useState<ManualRequestFilter>('all');
  const [searchText, setSearchText] = useState('');

  const [createForm, setCreateForm] =
    useState<CreateManualBackupRequestPayload>(initialCreateForm);

  const [selectedRequestId, setSelectedRequestId] = useState<string | null>(null);

  const [completeRequestId, setCompleteRequestId] = useState<string | null>(null);
  const [validateRequestId, setValidateRequestId] = useState<string | null>(null);

  const [completeForm, setCompleteForm] = useState({
    backupType: 'ManualDiskClone',
    backupPath: '',
    completionNotes: '',
  });

  const [validateForm, setValidateForm] = useState({
    validatedBy: '',
    validationNotes: '',
  });

  const [isLoading, setIsLoading] = useState(true);
  const [isCreating, setIsCreating] = useState(false);
  const [actionInProgressId, setActionInProgressId] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  async function loadData() {
    try {
      setIsLoading(true);
      setErrorMessage(null);

      const [requestsResponse, systemsResponse] = await Promise.all([
        sbcApi.get<ManualBackupRequest[]>('/manual-backup-requests'),
        sbcApi.get<UrBackupSystem[]>('/dashboard/urbackup-systems'),
      ]);

      setRequests(requestsResponse.data);
      setSystems(systemsResponse.data);
    } catch (error) {
      console.error(error);
      setErrorMessage('Could not load manual backup requests.');
    } finally {
      setIsLoading(false);
    }
  }

  async function createRequest(event: SyntheticEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!createForm.protectedSystemId || !createForm.requestedBy || !createForm.reason) {
      setErrorMessage('Protected system, requested by and reason are required.');
      return;
    }

    try {
      setIsCreating(true);
      setErrorMessage(null);

      await sbcApi.post('/manual-backup-requests', createForm);

      setCreateForm(initialCreateForm);
      await loadData();
    } catch (error) {
      console.error(error);
      setErrorMessage('Could not create manual backup request.');
    } finally {
      setIsCreating(false);
    }
  }

  async function startRequest(requestId: string) {
    try {
      setActionInProgressId(requestId);
      setErrorMessage(null);

      await sbcApi.put(`/manual-backup-requests/${requestId}/start`);

      await loadData();
    } catch (error) {
      console.error(error);
      setErrorMessage('Could not start manual backup request.');
    } finally {
      setActionInProgressId(null);
    }
  }

  function openCompleteModal(requestId: string) {
    setCompleteRequestId(requestId);
    setCompleteForm({
      backupType: 'ManualDiskClone',
      backupPath: '',
      completionNotes: '',
    });
  }

  async function completeRequest() {
    if (!completeRequestId) {
      return;
    }

    if (!completeForm.backupPath || !completeForm.completionNotes) {
      setErrorMessage('Backup path and completion notes are required.');
      return;
    }

    try {
      setActionInProgressId(completeRequestId);
      setErrorMessage(null);

      await sbcApi.put(`/manual-backup-requests/${completeRequestId}/complete`, {
        backupType: completeForm.backupType,
        startedAtUtc: new Date().toISOString(),
        backupPath: completeForm.backupPath,
        completionNotes: completeForm.completionNotes,
      });

      setCompleteRequestId(null);
      await loadData();
    } catch (error) {
      console.error(error);
      setErrorMessage('Could not complete manual backup request.');
    } finally {
      setActionInProgressId(null);
    }
  }

  function openValidateModal(requestId: string) {
    setValidateRequestId(requestId);
    setValidateForm({
      validatedBy: '',
      validationNotes: '',
    });
  }

  async function validateRequest() {
    if (!validateRequestId) {
      return;
    }

    if (!validateForm.validatedBy || !validateForm.validationNotes) {
      setErrorMessage('Validated by and validation notes are required.');
      return;
    }

    try {
      setActionInProgressId(validateRequestId);
      setErrorMessage(null);

      await sbcApi.put(`/manual-backup-requests/${validateRequestId}/validate`, {
        validatedBy: validateForm.validatedBy,
        validationNotes: validateForm.validationNotes,
      });

      setValidateRequestId(null);
      await loadData();
    } catch (error) {
      console.error(error);
      setErrorMessage('Could not validate manual backup request.');
    } finally {
      setActionInProgressId(null);
    }
  }

  useEffect(() => {
    loadData();
  }, []);

  const filteredRequests = useMemo(() => {
    const normalizedSearchText = searchText.trim().toLowerCase();

    return requests.filter((request) => {
      const matchesFilter =
        filter === 'all' ||
        request.status === filter;

      const searchableValues = [
        request.requestedBy,
        request.assignedTo,
        request.reason,
        request.relatedChangeReference,
        request.status,
        request.backupType,
        request.backupPath,
        request.completionNotes,
        request.validationNotes,
        request.validatedBy,
        request.protectedSystem?.hostname,
        request.protectedSystem?.operatingSystem,
        request.protectedSystem?.ipAddress,
        request.protectedSystem?.simulator?.name,
      ]
        .filter((value): value is string => Boolean(value))
        .map((value) => value.toLowerCase());

      const matchesSearch =
        normalizedSearchText.length === 0 ||
        searchableValues.some((value) => value.includes(normalizedSearchText));

      return matchesFilter && matchesSearch;
    });
  }, [requests, filter, searchText]);

  const selectedRequest =
    requests.find((request) => request.id === selectedRequestId) ?? null;

  const pendingCount = requests.filter((request) => request.status === 'Pending').length;
  const inProgressCount = requests.filter((request) => request.status === 'InProgress').length;
  const completedCount = requests.filter((request) => request.status === 'Completed').length;
  const validatedCount = requests.filter((request) => request.status === 'Validated').length;
  const cancelledCount = requests.filter((request) => request.status === 'Cancelled').length;

  const manualCandidateSystems = systems.filter(
    (system) => !system.urBackupClientId && !system.urBackupClientName
  );

  return (
    <div className="page-shell">
      <header className="page-title-row">
        <div>
          <h2>Manual Requests</h2>
          <p>Manual backup workflow for systems not fully covered by UrBackup.</p>
        </div>

        <button
          className="primary-button"
          type="button"
          onClick={loadData}
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
          title="Pending"
          value={pendingCount}
          description="Requests waiting to start"
          variant={pendingCount > 0 ? 'warning' : 'default'}
        />

        <SummaryCard
          title="In progress"
          value={inProgressCount}
          description="Manual backups currently in progress"
          variant={inProgressCount > 0 ? 'warning' : 'default'}
        />

        <SummaryCard
          title="Completed"
          value={completedCount}
          description="Completed but not validated"
          variant={completedCount > 0 ? 'warning' : 'default'}
        />

        <SummaryCard
          title="Validated"
          value={validatedCount}
          description="Reviewed and accepted backups"
        />

        <SummaryCard
          title="Cancelled"
          value={cancelledCount}
          description="Cancelled manual requests"
          variant={cancelledCount > 0 ? 'danger' : 'default'}
        />
      </section>

      <section className="dashboard-section">
        <div className="section-header">
          <div>
            <h3>Create manual backup request</h3>
            <p>Register a manual backup task for systems not handled automatically.</p>
          </div>
        </div>

        <form className="manual-request-form" onSubmit={createRequest}>
          <select
            className="filter-select"
            value={createForm.protectedSystemId}
            onChange={(event) =>
              setCreateForm((current) => ({
                ...current,
                protectedSystemId: event.target.value,
              }))
            }
          >
            <option value="">Select protected system</option>

            {manualCandidateSystems.map((system) => (
              <option key={system.id} value={system.id}>
                {system.hostname}
              </option>
            ))}

            {manualCandidateSystems.length === 0 && (
              <option value="" disabled>
                No manual candidates detected
              </option>
            )}
          </select>

          <input
            className="filter-input"
            type="text"
            placeholder="Requested by"
            value={createForm.requestedBy}
            onChange={(event) =>
              setCreateForm((current) => ({
                ...current,
                requestedBy: event.target.value,
              }))
            }
          />

          <input
            className="filter-input"
            type="text"
            placeholder="Assigned to"
            value={createForm.assignedTo}
            onChange={(event) =>
              setCreateForm((current) => ({
                ...current,
                assignedTo: event.target.value,
              }))
            }
          />

          <input
            className="filter-input"
            type="text"
            placeholder="Change reference"
            value={createForm.relatedChangeReference}
            onChange={(event) =>
              setCreateForm((current) => ({
                ...current,
                relatedChangeReference: event.target.value,
              }))
            }
          />

          <textarea
            className="filter-textarea"
            placeholder="Reason"
            value={createForm.reason}
            onChange={(event) =>
              setCreateForm((current) => ({
                ...current,
                reason: event.target.value,
              }))
            }
          />

          <button
            className="primary-button"
            type="submit"
            disabled={isCreating}
          >
            {isCreating ? 'Creating...' : 'Create request'}
          </button>
        </form>
      </section>

      <section className="dashboard-section">
        <div className="section-header">
          <div>
            <h3>Manual backup requests</h3>
            <p>Track requested, started, completed and validated manual backups.</p>
          </div>

          <span className="section-count">
            {filteredRequests.length}
          </span>
        </div>

        <div className="filters-row">
          <input
            className="filter-input"
            type="search"
            placeholder="Search by requester, assignee, reason, reference, system or notes..."
            value={searchText}
            onChange={(event) => setSearchText(event.target.value)}
          />

          <select
            className="filter-select"
            value={filter}
            onChange={(event) => setFilter(event.target.value as ManualRequestFilter)}
          >
            <option value="all">All statuses</option>
            <option value="Pending">Pending</option>
            <option value="InProgress">In progress</option>
            <option value="Completed">Completed</option>
            <option value="Validated">Validated</option>
            <option value="Cancelled">Cancelled</option>
          </select>
        </div>

        {isLoading && requests.length === 0 ? (
          <section className="status-card">
            Loading manual backup requests...
          </section>
        ) : (
          <ManualRequestsTable
            requests={filteredRequests}
            actionInProgressId={actionInProgressId}
            onStart={startRequest}
            onComplete={openCompleteModal}
            onValidate={openValidateModal}
            onSelect={setSelectedRequestId}
          />
        )}
      </section>

      {selectedRequest && (
        <section className="dashboard-section">
          <div className="section-header">
            <div>
              <h3>Selected request detail</h3>
              <p>{selectedRequest.reason}</p>
            </div>

            <button
              className="secondary-button"
              type="button"
              onClick={() => setSelectedRequestId(null)}
            >
              Clear selection
            </button>
          </div>

          <div className="detail-grid">
            <DetailItem
              label="System"
              value={selectedRequest.protectedSystem?.hostname ?? selectedRequest.protectedSystemId}
            />

            <DetailItem
              label="Operating system"
              value={selectedRequest.protectedSystem?.operatingSystem ?? 'Unknown OS'}
            />

            <DetailItem
              label="Status"
              value={formatReason(selectedRequest.status)}
            />

            <DetailItem
              label="Requested by"
              value={selectedRequest.requestedBy}
            />

            <DetailItem
              label="Assigned to"
              value={selectedRequest.assignedTo ?? 'Not assigned'}
            />

            <DetailItem
              label="Requested at"
              value={formatDateTime(selectedRequest.requestedAtUtc)}
            />

            <DetailItem
              label="Started at"
              value={formatDateTime(selectedRequest.startedAtUtc)}
            />

            <DetailItem
              label="Completed at"
              value={formatDateTime(selectedRequest.completedAtUtc)}
            />

            <DetailItem
              label="Validated at"
              value={formatDateTime(selectedRequest.validatedAtUtc)}
            />

            <DetailItem
              label="Backup type"
              value={selectedRequest.backupType ? formatReason(selectedRequest.backupType) : 'Not available'}
            />

            <DetailItem
              label="Backup path"
              value={selectedRequest.backupPath ?? 'Not available'}
            />

            <DetailItem
              label="Validated by"
              value={selectedRequest.validatedBy ?? 'Not available'}
            />

            <DetailItem
              label="Change reference"
              value={selectedRequest.relatedChangeReference ?? 'Not available'}
            />

            <DetailItem
              label="Completion notes"
              value={selectedRequest.completionNotes ?? 'Not available'}
            />

            <DetailItem
              label="Validation notes"
              value={selectedRequest.validationNotes ?? 'Not available'}
            />
          </div>
        </section>
      )}

      {completeRequestId && (
        <CompleteRequestModal
          form={completeForm}
          isSubmitting={actionInProgressId === completeRequestId}
          onChange={setCompleteForm}
          onCancel={() => setCompleteRequestId(null)}
          onSubmit={completeRequest}
        />
      )}

      {validateRequestId && (
        <ValidateRequestModal
          form={validateForm}
          isSubmitting={actionInProgressId === validateRequestId}
          onChange={setValidateForm}
          onCancel={() => setValidateRequestId(null)}
          onSubmit={validateRequest}
        />
      )}
    </div>
  );
}

type ManualRequestsTableProps = {
  requests: ManualBackupRequest[];
  actionInProgressId: string | null;
  onStart: (requestId: string) => void;
  onComplete: (requestId: string) => void;
  onValidate: (requestId: string) => void;
  onSelect: (requestId: string) => void;
};

function ManualRequestsTable({
  requests,
  actionInProgressId,
  onStart,
  onComplete,
  onValidate,
  onSelect,
}: ManualRequestsTableProps) {
  if (requests.length === 0) {
    return (
      <EmptyState message="No manual backup requests match the selected filters." />
    );
  }

  return (
    <div className="table-wrapper">
      <table className="data-table">
        <thead>
          <tr>
            <th>System</th>
            <th>Status</th>
            <th>Requested by</th>
            <th>Assigned to</th>
            <th>Requested</th>
            <th>Reason</th>
            <th>Actions</th>
          </tr>
        </thead>

        <tbody>
          {requests.map((request) => (
            <tr key={request.id}>
              <td>
                <strong>{request.protectedSystem?.hostname ?? request.protectedSystemId}</strong>
                <span>{request.protectedSystem?.operatingSystem ?? 'Unknown OS'}</span>

                {request.protectedSystem?.simulator && (
                  <span>{request.protectedSystem.simulator.name}</span>
                )}

                {request.protectedSystem && (
                  <Link
                    className="inline-link"
                    to={`/systems/${request.protectedSystem.id}`}
                  >
                    View system
                  </Link>
                )}
              </td>

              <td>
                <StatusBadge value={request.status} />
              </td>

              <td>
                {request.requestedBy}
              </td>

              <td>
                {request.assignedTo || 'Not assigned'}
              </td>

              <td>
                {formatDateTime(request.requestedAtUtc)}
              </td>

              <td>
                <span className="alert-message">
                  {request.reason}
                </span>
              </td>

              <td>
                <div className="table-action-group">
                  <button
                    className="table-action-button"
                    type="button"
                    onClick={() => onSelect(request.id)}
                  >
                    Details
                  </button>

                  {request.status === 'Pending' && (
                    <button
                      className="table-action-button"
                      type="button"
                      onClick={() => onStart(request.id)}
                      disabled={actionInProgressId === request.id}
                    >
                      {actionInProgressId === request.id ? 'Starting...' : 'Start'}
                    </button>
                  )}

                  {request.status === 'InProgress' && (
                    <button
                      className="table-action-button"
                      type="button"
                      onClick={() => onComplete(request.id)}
                      disabled={actionInProgressId === request.id}
                    >
                      Complete
                    </button>
                  )}

                  {request.status === 'Completed' && (
                    <button
                      className="table-action-button"
                      type="button"
                      onClick={() => onValidate(request.id)}
                      disabled={actionInProgressId === request.id}
                    >
                      Validate
                    </button>
                  )}
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

type CompleteForm = {
  backupType: string;
  backupPath: string;
  completionNotes: string;
};

type CompleteRequestModalProps = {
  form: CompleteForm;
  isSubmitting: boolean;
  onChange: (value: CompleteForm) => void;
  onCancel: () => void;
  onSubmit: () => void;
};

function CompleteRequestModal({
  form,
  isSubmitting,
  onChange,
  onCancel,
  onSubmit,
}: CompleteRequestModalProps) {
  return (
    <div className="modal-backdrop">
      <div className="modal-card">
        <h3>Complete manual backup</h3>
        <p>Register the backup evidence and completion notes.</p>

        <div className="modal-form">
          <select
            className="filter-select"
            value={form.backupType}
            onChange={(event) =>
              onChange({
                ...form,
                backupType: event.target.value,
              })
            }
          >
            <option value="ManualDiskClone">Manual disk clone</option>
            <option value="ManualOther">Manual other</option>
          </select>

          <input
            className="filter-input"
            type="text"
            placeholder="Backup path or evidence reference"
            value={form.backupPath}
            onChange={(event) =>
              onChange({
                ...form,
                backupPath: event.target.value,
              })
            }
          />

          <textarea
            className="filter-textarea"
            placeholder="Completion notes"
            value={form.completionNotes}
            onChange={(event) =>
              onChange({
                ...form,
                completionNotes: event.target.value,
              })
            }
          />
        </div>

        <div className="modal-actions">
          <button
            className="secondary-button"
            type="button"
            onClick={onCancel}
          >
            Cancel
          </button>

          <button
            className="primary-button"
            type="button"
            onClick={onSubmit}
            disabled={isSubmitting}
          >
            {isSubmitting ? 'Completing...' : 'Complete'}
          </button>
        </div>
      </div>
    </div>
  );
}

type ValidateForm = {
  validatedBy: string;
  validationNotes: string;
};

type ValidateRequestModalProps = {
  form: ValidateForm;
  isSubmitting: boolean;
  onChange: (value: ValidateForm) => void;
  onCancel: () => void;
  onSubmit: () => void;
};

function ValidateRequestModal({
  form,
  isSubmitting,
  onChange,
  onCancel,
  onSubmit,
}: ValidateRequestModalProps) {
  return (
    <div className="modal-backdrop">
      <div className="modal-card">
        <h3>Validate manual backup</h3>
        <p>Confirm that the manual backup was reviewed and accepted.</p>

        <div className="modal-form">
          <input
            className="filter-input"
            type="text"
            placeholder="Validated by"
            value={form.validatedBy}
            onChange={(event) =>
              onChange({
                ...form,
                validatedBy: event.target.value,
              })
            }
          />

          <textarea
            className="filter-textarea"
            placeholder="Validation notes"
            value={form.validationNotes}
            onChange={(event) =>
              onChange({
                ...form,
                validationNotes: event.target.value,
              })
            }
          />
        </div>

        <div className="modal-actions">
          <button
            className="secondary-button"
            type="button"
            onClick={onCancel}
          >
            Cancel
          </button>

          <button
            className="primary-button"
            type="button"
            onClick={onSubmit}
            disabled={isSubmitting}
          >
            {isSubmitting ? 'Validating...' : 'Validate'}
          </button>
        </div>
      </div>
    </div>
  );
}