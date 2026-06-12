import { NavLink } from 'react-router-dom';

const navigationItems = [
  {
    label: 'Overview',
    path: '/',
    description: 'General status',
  },
  {
    label: 'Systems',
    path: '/systems',
    description: 'Protected systems',
  },
  {
    label: 'Alerts',
    path: '/alerts',
    description: 'Open and resolved alerts',
  },
  {
    label: 'Backups',
    path: '/backups',
    description: 'Backup jobs',
  },
  {
    label: 'Manual Requests',
    path: '/manual-requests',
    description: 'Manual backup workflow',
  },
  {
    label: 'UrBackup',
    path: '/urbackup',
    description: 'Integration status',
  },
];

export function Sidebar() {
  return (
    <aside className="sidebar">
      <div className="sidebar-brand">
        <span className="brand-mark">SBC</span>
        <div>
          <h1>Simulator Backup Control</h1>
          <p>Backup monitoring</p>
        </div>
      </div>

      <nav className="sidebar-nav">
        {navigationItems.map((item) => (
          <NavLink
            key={item.path}
            to={item.path}
            end={item.path === '/'}
            className={({ isActive }) =>
              isActive ? 'sidebar-link sidebar-link--active' : 'sidebar-link'
            }
          >
            <strong>{item.label}</strong>
            <span>{item.description}</span>
          </NavLink>
        ))}
      </nav>
    </aside>
  );
}