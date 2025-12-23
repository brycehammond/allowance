import React, { useState } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import {
  LayoutDashboard,
  Users,
  UserPlus,
  LogOut,
  Menu,
  X,
  DollarSign,
  Key,
} from 'lucide-react';

interface LayoutProps {
  children: React.ReactNode;
}

export const Layout: React.FC<LayoutProps> = ({ children }) => {
  const { user, logout } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  const navigation = [
    { name: 'Dashboard', href: '/dashboard', icon: LayoutDashboard },
    { name: 'Add Child', href: '/children/add', icon: UserPlus },
    { name: 'Add Co-Parent', href: '/parent/add', icon: Users },
    { name: 'Change Password', href: '/change-password', icon: Key },
  ];

  const isActivePath = (path: string) => {
    return location.pathname === path;
  };

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Sidebar for desktop */}
      <div className="hidden md:fixed md:inset-y-0 md:flex md:w-64 md:flex-col">
        <div className="flex min-h-0 flex-1 flex-col bg-primary-700">
          <div className="flex flex-1 flex-col overflow-y-auto pt-5 pb-4">
            <div className="flex flex-shrink-0 items-center px-4">
              <div className="flex items-center">
                <DollarSign className="h-8 w-8 text-white" />
                <h1 className="ml-2 text-2xl font-bold text-white">
                  Earn &amp; Learn
                </h1>
              </div>
            </div>
            <nav className="mt-8 flex-1 space-y-1 px-2">
              {navigation.map((item) => {
                const Icon = item.icon;
                const active = isActivePath(item.href);
                return (
                  <Link
                    key={item.name}
                    to={item.href}
                    className={`
                      group flex items-center rounded-md px-3 py-2 text-sm font-medium transition-colors
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
                  <Users className="h-6 w-6 text-white" />
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

      {/* Mobile menu */}
      <div className="md:hidden">
        <div className="fixed top-0 left-0 right-0 z-40 flex items-center justify-between bg-primary-700 px-4 py-3 shadow-lg">
          <div className="flex items-center">
            <DollarSign className="h-6 w-6 text-white" />
            <h1 className="ml-2 text-lg font-bold text-white">Earn &amp; Learn</h1>
          </div>
          <button
            onClick={() => setMobileMenuOpen(!mobileMenuOpen)}
            className="rounded-md p-2 text-white hover:bg-primary-600"
          >
            {mobileMenuOpen ? (
              <X className="h-6 w-6" />
            ) : (
              <Menu className="h-6 w-6" />
            )}
          </button>
        </div>

        {/* Mobile menu overlay */}
        {mobileMenuOpen && (
          <div className="fixed inset-0 z-30 bg-gray-600 bg-opacity-75" onClick={() => setMobileMenuOpen(false)} />
        )}

        {/* Mobile menu panel */}
        <div
          className={`
            fixed top-0 left-0 z-40 h-full w-64 transform bg-primary-700 transition-transform duration-300 ease-in-out
            ${mobileMenuOpen ? 'translate-x-0' : '-translate-x-full'}
          `}
        >
          <div className="flex h-full flex-col">
            <div className="flex items-center px-4 py-4">
              <DollarSign className="h-8 w-8 text-white" />
              <h1 className="ml-2 text-xl font-bold text-white">
                Earn &amp; Learn
              </h1>
            </div>
            <nav className="mt-4 flex-1 space-y-1 px-2">
              {navigation.map((item) => {
                const Icon = item.icon;
                const active = isActivePath(item.href);
                return (
                  <Link
                    key={item.name}
                    to={item.href}
                    onClick={() => setMobileMenuOpen(false)}
                    className={`
                      group flex items-center rounded-md px-3 py-2 text-sm font-medium transition-colors
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
            <div className="flex flex-shrink-0 border-t border-primary-800 p-4">
              <div className="group block w-full flex-shrink-0">
                <div className="flex items-center">
                  <div className="flex h-10 w-10 items-center justify-center rounded-full bg-primary-600">
                    <Users className="h-6 w-6 text-white" />
                  </div>
                  <div className="ml-3 flex-1">
                    <p className="text-sm font-medium text-white">
                      {user?.firstName} {user?.lastName}
                    </p>
                    <p className="text-xs text-primary-200">{user?.role}</p>
                  </div>
                  <button
                    onClick={() => {
                      handleLogout();
                      setMobileMenuOpen(false);
                    }}
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
      </div>

      {/* Main content */}
      <div className="md:pl-64">
        <main className="py-6 md:py-10">
          <div className="mx-auto max-w-7xl px-4 sm:px-6 md:px-8 mt-16 md:mt-0">
            {children}
          </div>
        </main>
      </div>
    </div>
  );
};
