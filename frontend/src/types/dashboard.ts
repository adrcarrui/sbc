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