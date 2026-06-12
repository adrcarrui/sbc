export type DashboardSummary = {
  totalSystems: number;
  urBackupIntegratedSystems: number;
  manualOrPendingSystems: number;
  onlineSystems: number;
  offlineSystems: number;
  removedFromUrBackupSystems: number;
  backupOkSystems: number;
  backupWithIssuesSystems: number;
  noSuccessfulBackupSystems: number;
  openAlerts: number;
  lastUrBackupSyncAtUtc: string | null;
};

export type SimulatorSummary = {
  id: string;
  code: string;
  name: string;
};

export type AttentionSystem = {
  id: string;
  hostname: string;
  ipAddress: string | null;
  operatingSystem: string | null;
  urBackupClientId: string | null;
  urBackupClientName: string | null;
  isActive: boolean;
  isOnline: boolean;
  isRemovedFromUrBackup: boolean;
  lastUrBackupSyncAtUtc: string | null;
  lastSeenAtUtc: string | null;
  lastFileBackupAtUtc: string | null;
  lastImageBackupAtUtc: string | null;
  lastFileBackupOk: boolean;
  lastImageBackupOk: boolean;
  lastFileBackupIssues: number | null;
  severity: string;
  reason: string;
  description: string;
  simulator: SimulatorSummary | null;
};

export type BackupJobSummary = {
  id: string;
  source: string;
  backupType: string;
  status: string;
  startedAtUtc: string | null;
  finishedAtUtc: string | null;
  durationSeconds: number | null;
  sizeBytes: number | null;
  backupPath: string | null;
  urBackupJobId: string | null;
  errorMessage: string | null;
};

export type LatestBackupSystem = {
  id: string;
  hostname: string;
  ipAddress: string | null;
  operatingSystem: string | null;
  isActive: boolean;
  isOnline: boolean;
  isRemovedFromUrBackup: boolean;
  isIntegratedWithUrBackup: boolean;
  backupCapability: string;
  fileBackupValidated: boolean;
  imageBackupValidated: boolean;
  lastUrBackupSyncAtUtc: string | null;
  latestStatus: string;
  requiresAttention: boolean;
  latestFileBackup: BackupJobSummary | null;
  latestImageBackup: BackupJobSummary | null;
  latestAnyBackup: BackupJobSummary | null;
  simulator: SimulatorSummary | null;
};

export type ProtectedSystemSummary = {
  id: string;
  hostname: string;
  ipAddress: string | null;
  operatingSystem: string | null;
  isOnline: boolean;
  isRemovedFromUrBackup: boolean;
  simulator: SimulatorSummary | null;
};

export type OpenAlert = {
  id: string;
  code: string;
  title: string;
  message: string;
  severity: string;
  status: string;
  createdAtUtc: string;
  resolvedAtUtc: string | null;
  protectedSystem: ProtectedSystemSummary | null;
};

export type RecentBackupEvent = {
  id: string;
  eventType: string;
  severity: string;
  message: string;
  createdAtUtc: string;
  protectedSystem: ProtectedSystemSummary | null;
};

export type UrBackupSystem = {
  id: string;
  hostname: string;
  ipAddress: string | null;
  operatingSystem: string | null;
  urBackupClientId: string | null;
  urBackupClientName: string | null;
  urBackupClientVersion: string | null;
  isActive: boolean;
  isOnline: boolean;
  isRemovedFromUrBackup: boolean;
  lastUrBackupSyncAtUtc: string | null;
  lastSeenAtUtc: string | null;
  lastFileBackupAtUtc: string | null;
  lastImageBackupAtUtc: string | null;
  lastFileBackupOk: boolean;
  lastImageBackupOk: boolean;
  lastFileBackupIssues: number | null;
  urBackupStatusCode: number | null;
  operationalStatus: string;
  backupStatus: string;
  simulator: SimulatorSummary | null;
};

export type AlertSummary = {
  id: string;
  code: string;
  title: string;
  message: string;
  severity: string;
  status: string;
  createdAtUtc: string;
  resolvedAtUtc: string | null;
};

export type BackupEventSummary = {
  id: string;
  eventType: string;
  severity: string;
  message: string;
  createdAtUtc: string;
};

export type UrBackupSystemDetail = {
  id: string;
  hostname: string;
  ipAddress: string | null;
  operatingSystem: string | null;
  fileSystem: string | null;
  partitionScheme: string | null;
  notes: string | null;
  criticality: string;
  backupCapability: string;
  isActive: boolean;

  urBackup: {
    isIntegrated: boolean;
    urBackupClientId: string | null;
    urBackupClientName: string | null;
    urBackupClientVersion: string | null;
    isOnline: boolean;
    isRemovedFromUrBackup: boolean;
    lastUrBackupSyncAtUtc: string | null;
    removedFromUrBackupAtUtc: string | null;
    lastSeenAtUtc: string | null;
    lastFileBackupAtUtc: string | null;
    lastImageBackupAtUtc: string | null;
    lastFileBackupOk: boolean;
    lastImageBackupOk: boolean;
    lastFileBackupIssues: number | null;
    urBackupStatusCode: number | null;
    operationalStatus: string;
    backupStatus: string;
  };

  simulator: SimulatorSummary | null;
  openAlerts: AlertSummary[];
  recentAlerts: AlertSummary[];
  recentEvents: BackupEventSummary[];
};

export type UrBackupHealthResult = {
  isReachable: boolean;
  baseUrl: string;
  statusCode: number | null;
  errorMessage: string | null;
  checkedAtUtc: string;
};

export type UrBackupRawStatusResult = {
  success: boolean;
  apiUrl: string;
  rawJson: string | null;
  errorMessage: string | null;
  checkedAtUtc: string;
};

export type UrBackupSyncedClientResult = {
  urBackupClientId: string | null;
  name: string;
  online: boolean;
  operatingSystem: string | null;
  lastSeenAtUtc: string | null;
  lastFileBackupAtUtc: string | null;
  lastImageBackupAtUtc: string | null;
  fileBackupOk: boolean;
  imageBackupOk: boolean;
  isRemovedFromUrBackup: boolean;
  lastUrBackupSyncAtUtc: string | null;
  action: string;
};

export type UrBackupClientSyncResult = {
  success: boolean;
  message: string | null;
  errorMessage: string | null;
  discoveredClients: number;
  createdClients: number;
  updatedClients: number;
  restoredClients: number;
  removedClients: number;
  skippedClients: number;
  syncedClients: UrBackupSyncedClientResult[];
};

export type UrBackupRawClientStatus = {
  id: number;
  name: string;
  online: boolean;
  file_ok: boolean;
  image_ok: boolean;
  last_filebackup_issues: number;
  lastbackup: number;
  lastbackup_image: number;
  lastseen: number;
  os_simple: string | null;
  os_version_string: string | null;
  client_version_string: string | null;
  ip: string | null;
};

export type ManualBackupRequest = {
  id: string;
  protectedSystemId: string;
  requestedBy: string;
  assignedTo: string | null;
  reason: string;
  relatedChangeReference: string | null;
  status: string;
  requestedAtUtc: string;
  startedAtUtc: string | null;
  completedAtUtc: string | null;
  validatedAtUtc: string | null;
  validatedBy: string | null;
  validationNotes: string | null;
  backupType: string | null;
  backupPath: string | null;
  completionNotes: string | null;
  protectedSystem: ProtectedSystemSummary | null;
};

export type CreateManualBackupRequestPayload = {
  protectedSystemId: string;
  requestedBy: string;
  assignedTo: string;
  reason: string;
  relatedChangeReference: string;
};

export type CompleteManualBackupRequestPayload = {
  backupType: string;
  startedAtUtc: string;
  backupPath: string;
  completionNotes: string;
};

export type ValidateManualBackupRequestPayload = {
  validatedBy: string;
  validationNotes: string;
};