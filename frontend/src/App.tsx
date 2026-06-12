import { Navigate, Route, Routes } from 'react-router-dom';
import { AppLayout } from './layout/AppLayout';
import { AlertsPage } from './pages/AlertsPage';
import { BackupsPage } from './pages/BackupsPage';
import { ManualRequestsPage } from './pages/ManualRequestsPage';
import { OverviewPage } from './pages/OverviewPage';
import { SystemsPage } from './pages/SystemsPage';
import { SystemDetailPage } from './pages/SystemDetailPage';
import { UrBackupPage } from './pages/UrBackupPage';

function App() {
  return (
    <Routes>
      <Route element={<AppLayout />}>
        <Route index element={<OverviewPage />} />
        <Route path="systems" element={<SystemsPage />} />
        <Route path="systems/:id" element={<SystemDetailPage />} />
        <Route path="alerts" element={<AlertsPage />} />
        <Route path="backups" element={<BackupsPage />} />
        <Route path="manual-requests" element={<ManualRequestsPage />} />
        <Route path="urbackup" element={<UrBackupPage />} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Route>
    </Routes>
  );
}

export default App;