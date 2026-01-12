import React, { useEffect, useState, useCallback, useRef } from 'react';
import { useAuth } from '../../contexts/AuthContext';
import { tasksApi } from '../../services/api';
import type {
  ChoreTask,
  TaskCompletion,
  CreateTaskRequest,
  RecurrenceType,
  DayOfWeek,
} from '../../types';

interface ChoresTabProps {
  childId: string;
}

export const ChoresTab: React.FC<ChoresTabProps> = ({ childId }) => {
  const { user } = useAuth();
  const [tasks, setTasks] = useState<ChoreTask[]>([]);
  const [pendingApprovals, setPendingApprovals] = useState<TaskCompletion[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string>('');
  const [showAddForm, setShowAddForm] = useState(false);
  const [showCompleteModal, setShowCompleteModal] = useState<ChoreTask | null>(null);
  const [formData, setFormData] = useState<CreateTaskRequest>({
    childId,
    title: '',
    description: '',
    rewardAmount: 0,
    isRecurring: false,
    recurrenceType: undefined,
    recurrenceDay: undefined,
  });
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [completionNotes, setCompletionNotes] = useState('');
  const [completionPhoto, setCompletionPhoto] = useState<File | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const isParent = user?.role === 'Parent';

  const loadTasks = useCallback(async () => {
    try {
      setIsLoading(true);
      const data = await tasksApi.getAll(childId, 'Active');
      setTasks(data);

      if (isParent) {
        const approvals = await tasksApi.getPendingApprovals();
        // Filter to only show approvals for this child
        setPendingApprovals(approvals.filter(a => a.childId === childId));
      }
    } catch (err: unknown) {
      const errorMessage = err instanceof Error && 'response' in err
        ? (err as { response?: { data?: { error?: string } } }).response?.data?.error
        : undefined;
      setError(errorMessage || 'Failed to load tasks');
    } finally {
      setIsLoading(false);
    }
  }, [childId, isParent]);

  useEffect(() => {
    loadTasks();
  }, [loadTasks]);

  const handleCreateTask = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    if (formData.rewardAmount <= 0) {
      setError('Reward amount must be greater than 0');
      return;
    }

    setIsSubmitting(true);

    try {
      await tasksApi.create(formData);
      setShowAddForm(false);
      setFormData({
        childId,
        title: '',
        description: '',
        rewardAmount: 0,
        isRecurring: false,
        recurrenceType: undefined,
        recurrenceDay: undefined,
      });
      await loadTasks();
    } catch (err: unknown) {
      const errorMessage = err instanceof Error && 'response' in err
        ? (err as { response?: { data?: { error?: string } } }).response?.data?.error
        : undefined;
      setError(errorMessage || 'Failed to create task');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleCompleteTask = async () => {
    if (!showCompleteModal) return;

    setIsSubmitting(true);
    setError('');

    try {
      await tasksApi.complete(
        showCompleteModal.id,
        completionNotes || undefined,
        completionPhoto || undefined
      );
      setShowCompleteModal(null);
      setCompletionNotes('');
      setCompletionPhoto(null);
      await loadTasks();
    } catch (err: unknown) {
      const errorMessage = err instanceof Error && 'response' in err
        ? (err as { response?: { data?: { error?: string } } }).response?.data?.error
        : undefined;
      setError(errorMessage || 'Failed to complete task');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleReviewCompletion = async (completionId: string, isApproved: boolean, rejectionReason?: string) => {
    try {
      await tasksApi.reviewCompletion(completionId, { isApproved, rejectionReason });
      await loadTasks();
    } catch (err: unknown) {
      const errorMessage = err instanceof Error && 'response' in err
        ? (err as { response?: { data?: { error?: string } } }).response?.data?.error
        : undefined;
      setError(errorMessage || 'Failed to review completion');
    }
  };

  const handleArchiveTask = async (taskId: string) => {
    if (!confirm('Are you sure you want to archive this task?')) return;

    try {
      await tasksApi.archive(taskId);
      await loadTasks();
    } catch (err: unknown) {
      const errorMessage = err instanceof Error && 'response' in err
        ? (err as { response?: { data?: { error?: string } } }).response?.data?.error
        : undefined;
      setError(errorMessage || 'Failed to archive task');
    }
  };

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
    }).format(amount);
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  if (isLoading) {
    return (
      <div className="flex justify-center items-center py-12">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-500"></div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header with Add Task Button (Parents only) */}
      <div className="flex justify-between items-center">
        <h3 className="text-lg font-medium text-gray-900">Chores & Tasks</h3>
        {isParent && (
          <button
            onClick={() => setShowAddForm(!showAddForm)}
            className="inline-flex items-center px-4 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-primary-600 hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500"
          >
            {showAddForm ? (
              <>
                <svg className="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                </svg>
                Cancel
              </>
            ) : (
              <>
                <svg className="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
                </svg>
                Add Task
              </>
            )}
          </button>
        )}
      </div>

      {/* Error Display */}
      {error && (
        <div className="rounded-md bg-red-50 p-4">
          <div className="text-sm text-red-800">{error}</div>
        </div>
      )}

      {/* Add Task Form (Parents only) */}
      {showAddForm && isParent && (
        <div className="bg-white border border-gray-200 rounded-lg p-6">
          <h4 className="text-md font-medium text-gray-900 mb-4">New Task</h4>
          <form onSubmit={handleCreateTask} className="space-y-4">
            <div>
              <label htmlFor="title" className="block text-sm font-medium text-gray-700">
                Task Title
              </label>
              <input
                type="text"
                id="title"
                required
                value={formData.title}
                onChange={(e) => setFormData({ ...formData, title: e.target.value })}
                className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                placeholder="e.g., Clean bedroom, Take out trash"
              />
            </div>

            <div>
              <label htmlFor="description" className="block text-sm font-medium text-gray-700">
                Description (optional)
              </label>
              <textarea
                id="description"
                rows={2}
                value={formData.description || ''}
                onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                placeholder="Any additional details..."
              />
            </div>

            <div>
              <label htmlFor="rewardAmount" className="block text-sm font-medium text-gray-700">
                Reward Amount
              </label>
              <div className="mt-1 relative rounded-md shadow-sm">
                <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                  <span className="text-gray-500 sm:text-sm">$</span>
                </div>
                <input
                  type="number"
                  id="rewardAmount"
                  step="0.01"
                  min="0.01"
                  required
                  value={formData.rewardAmount || ''}
                  onChange={(e) => setFormData({ ...formData, rewardAmount: parseFloat(e.target.value) || 0 })}
                  className="block w-full pl-7 pr-12 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                />
              </div>
            </div>

            <div className="flex items-center">
              <input
                type="checkbox"
                id="isRecurring"
                checked={formData.isRecurring}
                onChange={(e) => setFormData({
                  ...formData,
                  isRecurring: e.target.checked,
                  recurrenceType: e.target.checked ? 'Weekly' : undefined,
                  recurrenceDay: e.target.checked ? 'Monday' : undefined,
                })}
                className="h-4 w-4 text-primary-600 focus:ring-primary-500 border-gray-300 rounded"
              />
              <label htmlFor="isRecurring" className="ml-2 block text-sm text-gray-900">
                This is a recurring task
              </label>
            </div>

            {formData.isRecurring && (
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label htmlFor="recurrenceType" className="block text-sm font-medium text-gray-700">
                    Frequency
                  </label>
                  <select
                    id="recurrenceType"
                    value={formData.recurrenceType || 'Weekly'}
                    onChange={(e) => setFormData({ ...formData, recurrenceType: e.target.value as RecurrenceType })}
                    className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                  >
                    <option value="Daily">Daily</option>
                    <option value="Weekly">Weekly</option>
                    <option value="Monthly">Monthly</option>
                  </select>
                </div>

                {formData.recurrenceType === 'Weekly' && (
                  <div>
                    <label htmlFor="recurrenceDay" className="block text-sm font-medium text-gray-700">
                      Day of Week
                    </label>
                    <select
                      id="recurrenceDay"
                      value={formData.recurrenceDay || 'Monday'}
                      onChange={(e) => setFormData({ ...formData, recurrenceDay: e.target.value as DayOfWeek })}
                      className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                    >
                      <option value="Sunday">Sunday</option>
                      <option value="Monday">Monday</option>
                      <option value="Tuesday">Tuesday</option>
                      <option value="Wednesday">Wednesday</option>
                      <option value="Thursday">Thursday</option>
                      <option value="Friday">Friday</option>
                      <option value="Saturday">Saturday</option>
                    </select>
                  </div>
                )}

                {formData.recurrenceType === 'Monthly' && (
                  <div>
                    <label htmlFor="recurrenceDayOfMonth" className="block text-sm font-medium text-gray-700">
                      Day of Month
                    </label>
                    <input
                      type="number"
                      id="recurrenceDayOfMonth"
                      min="1"
                      max="28"
                      value={formData.recurrenceDayOfMonth || 1}
                      onChange={(e) => setFormData({ ...formData, recurrenceDayOfMonth: parseInt(e.target.value) || 1 })}
                      className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                    />
                  </div>
                )}
              </div>
            )}

            <div className="flex justify-end space-x-3">
              <button
                type="button"
                onClick={() => {
                  setShowAddForm(false);
                  setError('');
                }}
                className="px-4 py-2 border border-gray-300 shadow-sm text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={isSubmitting}
                className="px-4 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-primary-600 hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500 disabled:opacity-50"
              >
                {isSubmitting ? 'Creating...' : 'Create Task'}
              </button>
            </div>
          </form>
        </div>
      )}

      {/* Pending Approvals Section (Parents only) */}
      {isParent && pendingApprovals.length > 0 && (
        <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-6">
          <h4 className="text-md font-medium text-yellow-800 mb-4 flex items-center">
            <svg className="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
            Pending Approvals ({pendingApprovals.length})
          </h4>
          <div className="space-y-4">
            {pendingApprovals.map((completion) => (
              <div key={completion.id} className="bg-white rounded-lg p-4 border border-yellow-200">
                <div className="flex justify-between items-start">
                  <div>
                    <h5 className="font-medium text-gray-900">{completion.taskTitle}</h5>
                    <p className="text-sm text-gray-600">Completed: {formatDate(completion.completedAt)}</p>
                    {completion.notes && (
                      <p className="text-sm text-gray-500 mt-1">Notes: {completion.notes}</p>
                    )}
                    <p className="text-sm font-medium text-green-600 mt-1">
                      Reward: {formatCurrency(completion.rewardAmount)}
                    </p>
                  </div>
                  {completion.photoUrl && (
                    <a
                      href={completion.photoUrl}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="flex-shrink-0 ml-4"
                    >
                      <img
                        src={completion.photoUrl}
                        alt="Completion proof"
                        className="w-20 h-20 object-cover rounded-lg border border-gray-200"
                      />
                    </a>
                  )}
                </div>
                <div className="flex space-x-2 mt-4">
                  <button
                    onClick={() => handleReviewCompletion(completion.id, true)}
                    className="flex-1 px-3 py-2 border border-transparent text-sm font-medium rounded-md text-white bg-green-600 hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-green-500"
                  >
                    Approve
                  </button>
                  <button
                    onClick={() => {
                      const reason = prompt('Rejection reason (optional):');
                      handleReviewCompletion(completion.id, false, reason || undefined);
                    }}
                    className="flex-1 px-3 py-2 border border-red-300 text-sm font-medium rounded-md text-red-700 bg-white hover:bg-red-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-red-500"
                  >
                    Reject
                  </button>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Tasks List */}
      {tasks.length === 0 ? (
        <div className="text-center py-12 bg-white rounded-lg border border-gray-200">
          <svg
            className="mx-auto h-12 w-12 text-gray-400"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-6 9l2 2 4-4"
            />
          </svg>
          <h3 className="mt-2 text-sm font-medium text-gray-900">No tasks yet</h3>
          <p className="mt-1 text-sm text-gray-500">
            {isParent ? 'Create tasks for your child to earn rewards!' : 'No tasks assigned yet.'}
          </p>
        </div>
      ) : (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {tasks.map((task) => (
            <div
              key={task.id}
              className="bg-white rounded-lg shadow-sm border border-gray-200 overflow-hidden hover:shadow-md transition-shadow"
            >
              <div className="p-4">
                <div className="flex items-start justify-between mb-2">
                  <h4 className="text-lg font-medium text-gray-900">{task.title}</h4>
                  {task.isRecurring && (
                    <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-blue-100 text-blue-800">
                      <svg className="w-3 h-3 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                      </svg>
                      {task.recurrenceDisplay}
                    </span>
                  )}
                </div>

                {task.description && (
                  <p className="text-sm text-gray-600 mb-3">{task.description}</p>
                )}

                <div className="flex items-center justify-between mb-4">
                  <span className="text-lg font-bold text-green-600">
                    {formatCurrency(task.rewardAmount)}
                  </span>
                  <div className="text-xs text-gray-500">
                    {task.totalCompletions} completions
                  </div>
                </div>

                {task.pendingApprovals > 0 && (
                  <div className="mb-3 p-2 bg-yellow-50 rounded text-xs text-yellow-800">
                    {task.pendingApprovals} pending approval{task.pendingApprovals > 1 ? 's' : ''}
                  </div>
                )}

                <div className="flex space-x-2">
                  {!isParent && (
                    <button
                      onClick={() => setShowCompleteModal(task)}
                      className="flex-1 px-3 py-2 border border-transparent text-sm font-medium rounded-md text-white bg-green-600 hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-green-500"
                    >
                      Mark Complete
                    </button>
                  )}
                  {isParent && (
                    <>
                      <button
                        onClick={() => setShowCompleteModal(task)}
                        className="flex-1 px-3 py-2 border border-gray-300 text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500"
                      >
                        Complete
                      </button>
                      <button
                        onClick={() => handleArchiveTask(task.id)}
                        className="px-3 py-2 border border-red-300 text-sm font-medium rounded-md text-red-700 bg-white hover:bg-red-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-red-500"
                      >
                        <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 8h14M5 8a2 2 0 110-4h14a2 2 0 110 4M5 8v10a2 2 0 002 2h10a2 2 0 002-2V8m-9 4h4" />
                        </svg>
                      </button>
                    </>
                  )}
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Complete Task Modal */}
      {showCompleteModal && (
        <div className="fixed inset-0 z-50 overflow-y-auto">
          <div className="flex items-end justify-center min-h-screen pt-4 px-4 pb-20 text-center sm:block sm:p-0">
            <div className="fixed inset-0 bg-gray-500 bg-opacity-75 transition-opacity" onClick={() => setShowCompleteModal(null)} />

            <span className="hidden sm:inline-block sm:align-middle sm:h-screen">&#8203;</span>

            <div className="inline-block align-bottom bg-white rounded-lg px-4 pt-5 pb-4 text-left overflow-hidden shadow-xl transform transition-all sm:my-8 sm:align-middle sm:max-w-lg sm:w-full sm:p-6">
              <div>
                <div className="mx-auto flex items-center justify-center h-12 w-12 rounded-full bg-green-100">
                  <svg className="h-6 w-6 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                  </svg>
                </div>
                <div className="mt-3 text-center sm:mt-5">
                  <h3 className="text-lg leading-6 font-medium text-gray-900">
                    Complete Task: {showCompleteModal.title}
                  </h3>
                  <p className="mt-2 text-sm text-gray-500">
                    Reward: {formatCurrency(showCompleteModal.rewardAmount)}
                  </p>
                </div>
              </div>

              <div className="mt-5 space-y-4">
                <div>
                  <label htmlFor="completionNotes" className="block text-sm font-medium text-gray-700">
                    Notes (optional)
                  </label>
                  <textarea
                    id="completionNotes"
                    rows={2}
                    value={completionNotes}
                    onChange={(e) => setCompletionNotes(e.target.value)}
                    className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                    placeholder="Any notes about completing this task..."
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Photo Proof (optional)
                  </label>
                  <input
                    type="file"
                    ref={fileInputRef}
                    accept="image/jpeg,image/png,image/gif,image/webp"
                    onChange={(e) => setCompletionPhoto(e.target.files?.[0] || null)}
                    className="hidden"
                  />
                  {completionPhoto ? (
                    <div className="relative">
                      <img
                        src={URL.createObjectURL(completionPhoto)}
                        alt="Preview"
                        className="w-full h-48 object-cover rounded-lg"
                      />
                      <button
                        onClick={() => {
                          setCompletionPhoto(null);
                          if (fileInputRef.current) fileInputRef.current.value = '';
                        }}
                        className="absolute top-2 right-2 p-1 bg-red-500 text-white rounded-full hover:bg-red-600"
                      >
                        <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                        </svg>
                      </button>
                    </div>
                  ) : (
                    <button
                      type="button"
                      onClick={() => fileInputRef.current?.click()}
                      className="w-full flex justify-center items-center px-4 py-6 border-2 border-dashed border-gray-300 rounded-lg hover:border-primary-500 transition-colors"
                    >
                      <div className="text-center">
                        <svg className="mx-auto h-8 w-8 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 9a2 2 0 012-2h.93a2 2 0 001.664-.89l.812-1.22A2 2 0 0110.07 4h3.86a2 2 0 011.664.89l.812 1.22A2 2 0 0018.07 7H19a2 2 0 012 2v9a2 2 0 01-2 2H5a2 2 0 01-2-2V9z" />
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 13a3 3 0 11-6 0 3 3 0 016 0z" />
                        </svg>
                        <p className="mt-1 text-sm text-gray-600">Click to upload photo</p>
                      </div>
                    </button>
                  )}
                </div>
              </div>

              <div className="mt-5 sm:mt-6 sm:grid sm:grid-cols-2 sm:gap-3 sm:grid-flow-row-dense">
                <button
                  type="button"
                  onClick={handleCompleteTask}
                  disabled={isSubmitting}
                  className="w-full inline-flex justify-center rounded-md border border-transparent shadow-sm px-4 py-2 bg-green-600 text-base font-medium text-white hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-green-500 sm:col-start-2 sm:text-sm disabled:opacity-50"
                >
                  {isSubmitting ? 'Submitting...' : 'Submit Completion'}
                </button>
                <button
                  type="button"
                  onClick={() => {
                    setShowCompleteModal(null);
                    setCompletionNotes('');
                    setCompletionPhoto(null);
                  }}
                  className="mt-3 w-full inline-flex justify-center rounded-md border border-gray-300 shadow-sm px-4 py-2 bg-white text-base font-medium text-gray-700 hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500 sm:mt-0 sm:col-start-1 sm:text-sm"
                >
                  Cancel
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
