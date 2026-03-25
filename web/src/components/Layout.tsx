import React from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import {
  LayoutDashboard,
  Users,
  UserPlus,
  LogOut,
  PiggyBank,
  Key,
  Trash2,
  ClipboardList,
  Settings,
} from 'lucide-react';

interface LayoutProps {
  children: React.ReactNode;
}

export const Layout: React.FC<LayoutProps> = ({ children }) => {
  const { user, logout } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  const sidebarNav = [
    { name: 'Dashboard', href: '/dashboard', icon: LayoutDashboard },
    { name: 'Add Child', href: '/children/add', icon: UserPlus },
    { name: 'Add Co-Parent', href: '/parent/add', icon: Users },
    { name: 'Change Password', href: '/change-password', icon: Key },
    { name: 'Delete Account', href: '/delete-account', icon: Trash2 },
  ];

  const bottomTabs = [
    { name: 'Home', href: '/dashboard', icon: LayoutDashboard },
    { name: 'Chores', href: '/dashboard', icon: ClipboardList },
    { name: 'Settings', href: '/change-password', icon: Settings },
  ];

  const isActivePath = (path: string) => {
    return location.pathname === path;
  };

  return (
    <div className="min-h-screen" style={{ backgroundColor: '#FDFCFB' }}>
      {/* Sidebar for desktop */}
      <div className="hidden md:fixed md:inset-y-0 md:flex md:w-64 md:flex-col">
        <div className="flex min-h-0 flex-1 flex-col bg-primary-700">
          <div className="flex flex-1 flex-col overflow-y-auto pt-5 pb-4">
            <div className="flex flex-shrink-0 items-center px-4">
              <div className="flex items-center">
                <PiggyBank className="h-8 w-8 text-white" />
                <h1 className="ml-2 text-xl font-bold text-white font-headline">
                  Earn &amp; Learn
                </h1>
              </div>
            </div>
            <nav className="mt-8 flex-1 space-y-1 px-2">
              {sidebarNav.map((item) => {
                const Icon = item.icon;
                const active = isActivePath(item.href);
                return (
                  <Link
                    key={item.name}
                    to={item.href}
                    className={`
                      group flex items-center rounded-xl px-3 py-2.5 text-sm font-medium transition-colors
                      ${
                        active
                          ? 'bg-primary-800 text-white'
                          : 'text-primary-100 hover:bg-primary-600 hover:text-white'
                      }
                    `}
                  >
                    <Icon
                      className={`mr-3 h-5 w-5 flex-shrink-0 ${
                        active ? 'text-white' : 'text-primary-300 group-hover:text-white'
                      }`}
                    />
                    {item.name}
                  </Link>
                );
              })}
            </nav>
          </div>
          <div className="flex flex-shrink-0 border-t border-primary-800 p-4">
            <div className="group block w-full flex-shrink-0">
              <div className="flex items-center">
                <div className="flex h-10 w-10 items-center justify-center rounded-full bg-primary-600">
                  <span className="text-sm font-semibold text-white">
                    {user?.firstName?.charAt(0)}{user?.lastName?.charAt(0)}
                  </span>
                </div>
                <div className="ml-3 flex-1">
                  <p className="text-sm font-medium text-white">
                    {user?.firstName} {user?.lastName}
                  </p>
                  <p className="text-xs text-primary-200">{user?.role}</p>
                </div>
                <button
                  onClick={handleLogout}
                  className="ml-2 rounded-md p-2 text-primary-200 hover:bg-primary-600 hover:text-white transition-colors"
                  title="Logout"
                >
                  <LogOut className="h-5 w-5" />
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Mobile top bar */}
      <div className="md:hidden fixed top-0 left-0 right-0 z-40 flex items-center justify-between px-4 py-3 bg-white/80 backdrop-blur-md border-b border-gray-100">
        <div className="flex items-center gap-2">
          <PiggyBank className="h-6 w-6 text-primary-600" />
          <h1 className="text-lg font-bold text-gray-900 font-headline">Earn &amp; Learn</h1>
        </div>
        <button
          onClick={handleLogout}
          className="rounded-lg p-2 text-gray-400 hover:text-gray-600 hover:bg-gray-100 transition-colors"
          title="Logout"
        >
          <LogOut className="h-5 w-5" />
        </button>
      </div>

      {/* Mobile bottom tab bar */}
      <div className="md:hidden fixed bottom-0 left-0 right-0 z-40 bg-white border-t border-gray-100 safe-area-bottom">
        <nav className="flex justify-around px-2 py-1.5">
          {bottomTabs.map((item) => {
            const Icon = item.icon;
            const active = isActivePath(item.href) && item.name === 'Home'
              ? location.pathname === '/dashboard' || location.pathname.startsWith('/children/')
              : isActivePath(item.href);
            return (
              <Link
                key={item.name}
                to={item.href}
                className={`flex flex-col items-center gap-0.5 px-3 py-1.5 rounded-lg transition-colors min-w-[60px] ${
                  active ? 'text-primary-600' : 'text-gray-400'
                }`}
              >
                <Icon className="h-5 w-5" />
                <span className="text-[10px] font-medium">{item.name}</span>
              </Link>
            );
          })}
        </nav>
      </div>

      {/* Main content */}
      <div className="md:pl-64">
        <main className="py-6 md:py-10 mt-14 md:mt-0 pb-24 md:pb-10">
          <div className="mx-auto max-w-7xl px-4 sm:px-6 md:px-8">
            {children}
          </div>
        </main>
      </div>
    </div>
  );
};
