import React, { useEffect, useState, useCallback } from 'react';
import { Heart, Send, Edit, Clock, CheckCircle, AlertCircle } from 'lucide-react';
import { thankYouNotesApi } from '../../services/api';
import type { PendingThankYou, CreateThankYouNoteRequest, ThankYouNote } from '../../types';

interface ThankYouNotesTabProps {
  childId: string;
}

const occasionLabels: Record<string, string> = {
  Birthday: 'Birthday',
  Christmas: 'Christmas',
  Hanukkah: 'Hanukkah',
  Easter: 'Easter',
  Graduation: 'Graduation',
  GoodGrades: 'Good Grades',
  Holiday: 'Holiday',
  JustBecause: 'Just Because',
  Reward: 'Reward',
  Other: 'Other',
};

export const ThankYouNotesTab: React.FC<ThankYouNotesTabProps> = ({ childId }) => {
  const [pendingThankYous, setPendingThankYous] = useState<PendingThankYou[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string>('');
  const [success, setSuccess] = useState<string>('');
  const [selectedGift, setSelectedGift] = useState<PendingThankYou | null>(null);
  const [existingNote, setExistingNote] = useState<ThankYouNote | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [formData, setFormData] = useState<CreateThankYouNoteRequest>({
    message: '',
    imageUrl: '',
  });

  // Suppress unused variable warning - childId may be used for filtering in future
  void childId;

  const loadPendingThankYous = useCallback(async () => {
    try {
      setIsLoading(true);
      const data = await thankYouNotesApi.getPending();
      setPendingThankYous(data);
    } catch (err: unknown) {
      const errorMessage = err instanceof Error && 'response' in err
        ? (err as { response?: { data?: { message?: string } } }).response?.data?.message
        : undefined;
      setError(errorMessage || 'Failed to load pending thank yous');
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    loadPendingThankYous();
  }, [loadPendingThankYous]);

  const handleSelectGift = async (gift: PendingThankYou) => {
    setSelectedGift(gift);
    setError('');
    setSuccess('');

    // Check if a note already exists
    if (gift.hasNote) {
      try {
        const note = await thankYouNotesApi.getByGiftId(gift.giftId);
        setExistingNote(note);
        setFormData({
          message: note.message,
          imageUrl: note.imageUrl || '',
        });
      } catch {
        // Note doesn't exist yet, that's fine
        setExistingNote(null);
        setFormData({ message: '', imageUrl: '' });
      }
    } else {
      setExistingNote(null);
      setFormData({ message: '', imageUrl: '' });
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedGift) return;

    setError('');
    setSuccess('');
    setIsSubmitting(true);

    try {
      if (existingNote) {
        await thankYouNotesApi.update(selectedGift.giftId, formData);
        setSuccess('Thank you note updated!');
      } else {
        const note = await thankYouNotesApi.create(selectedGift.giftId, formData);
        setExistingNote(note);
        setSuccess('Thank you note created!');
      }
      await loadPendingThankYous();
    } catch (err: unknown) {
      const errorMessage = err instanceof Error && 'response' in err
        ? (err as { response?: { data?: { message?: string } } }).response?.data?.message
        : undefined;
      setError(errorMessage || 'Failed to save thank you note');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleSend = async () => {
    if (!selectedGift || !existingNote) return;

    setError('');
    setSuccess('');
    setIsSubmitting(true);

    try {
      await thankYouNotesApi.send(selectedGift.giftId);
      setSuccess('Thank you note sent!');
      setSelectedGift(null);
      setExistingNote(null);
      setFormData({ message: '', imageUrl: '' });
      await loadPendingThankYous();
    } catch (err: unknown) {
      const errorMessage = err instanceof Error && 'response' in err
        ? (err as { response?: { data?: { message?: string } } }).response?.data?.message
        : undefined;
      setError(errorMessage || 'Failed to send thank you note');
    } finally {
      setIsSubmitting(false);
    }
  };

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
    }).format(amount);
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString();
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
      {/* Header */}
      <div>
        <h3 className="text-lg font-medium text-gray-900">Thank You Notes</h3>
        <p className="text-sm text-gray-500">
          Write thank you notes to people who sent you gifts!
        </p>
      </div>

      {error && (
        <div className="rounded-md bg-red-50 p-4">
          <div className="flex items-center">
            <AlertCircle className="w-5 h-5 text-red-400 mr-2" />
            <div className="text-sm text-red-800">{error}</div>
          </div>
        </div>
      )}

      {success && (
        <div className="rounded-md bg-green-50 p-4">
          <div className="flex items-center">
            <CheckCircle className="w-5 h-5 text-green-400 mr-2" />
            <div className="text-sm text-green-800">{success}</div>
          </div>
        </div>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Pending Thank Yous List */}
        <div>
          <h4 className="text-sm font-medium text-gray-700 mb-3">Gifts to thank</h4>
          {pendingThankYous.length === 0 ? (
            <div className="text-center py-12 bg-white rounded-lg border border-gray-200">
              <Heart className="mx-auto h-12 w-12 text-gray-400" />
              <h3 className="mt-2 text-sm font-medium text-gray-900">All caught up!</h3>
              <p className="mt-1 text-sm text-gray-500">
                You've thanked everyone who sent you a gift.
              </p>
            </div>
          ) : (
            <div className="space-y-3">
              {pendingThankYous.map((gift) => (
                <button
                  key={gift.giftId}
                  onClick={() => handleSelectGift(gift)}
                  className={`w-full text-left p-4 rounded-lg border-2 transition-colors ${
                    selectedGift?.giftId === gift.giftId
                      ? 'border-primary-500 bg-primary-50'
                      : gift.hasNote
                      ? 'border-green-200 bg-green-50 hover:border-green-300'
                      : 'border-gray-200 bg-white hover:border-gray-300'
                  }`}
                >
                  <div className="flex justify-between items-start">
                    <div>
                      <p className="font-medium text-gray-900">
                        {gift.giverName}
                        {gift.giverRelationship && (
                          <span className="text-gray-500 font-normal"> ({gift.giverRelationship})</span>
                        )}
                      </p>
                      <p className="text-sm text-gray-600">
                        {formatCurrency(gift.amount)} for{' '}
                        {gift.customOccasion || occasionLabels[gift.occasion] || gift.occasion}
                      </p>
                      <p className="text-xs text-gray-400 mt-1">
                        Received {formatDate(gift.receivedAt)}
                        {gift.daysSinceReceived > 0 && ` (${gift.daysSinceReceived} days ago)`}
                      </p>
                    </div>
                    <div className="flex items-center">
                      {gift.hasNote ? (
                        <span className="inline-flex items-center px-2 py-1 rounded text-xs bg-green-100 text-green-700">
                          <Edit className="w-3 h-3 mr-1" />
                          Draft
                        </span>
                      ) : gift.daysSinceReceived > 7 ? (
                        <span className="inline-flex items-center px-2 py-1 rounded text-xs bg-yellow-100 text-yellow-700">
                          <Clock className="w-3 h-3 mr-1" />
                          Waiting
                        </span>
                      ) : null}
                    </div>
                  </div>
                </button>
              ))}
            </div>
          )}
        </div>

        {/* Thank You Note Editor */}
        <div>
          <h4 className="text-sm font-medium text-gray-700 mb-3">
            {selectedGift ? `Write note to ${selectedGift.giverName}` : 'Select a gift to write a note'}
          </h4>
          {selectedGift ? (
            <div className="bg-white rounded-lg border border-gray-200 p-6">
              <form onSubmit={handleSubmit} className="space-y-4">
                <div>
                  <label htmlFor="message" className="block text-sm font-medium text-gray-700 mb-1">
                    Your Message
                  </label>
                  <textarea
                    id="message"
                    required
                    rows={6}
                    value={formData.message}
                    onChange={(e) => setFormData({ ...formData, message: e.target.value })}
                    className="block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                    placeholder={`Dear ${selectedGift.giverName},\n\nThank you so much for the wonderful gift!\n\n...`}
                    disabled={existingNote?.isSent}
                  />
                </div>

                <div>
                  <label htmlFor="imageUrl" className="block text-sm font-medium text-gray-700 mb-1">
                    Image URL (optional)
                  </label>
                  <input
                    type="url"
                    id="imageUrl"
                    value={formData.imageUrl || ''}
                    onChange={(e) => setFormData({ ...formData, imageUrl: e.target.value })}
                    className="block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                    placeholder="https://..."
                    disabled={existingNote?.isSent}
                  />
                  <p className="mt-1 text-xs text-gray-500">Add a picture to your thank you note</p>
                </div>

                {existingNote?.isSent ? (
                  <div className="rounded-md bg-green-50 p-4">
                    <div className="flex items-center">
                      <CheckCircle className="w-5 h-5 text-green-400 mr-2" />
                      <div className="text-sm text-green-800">
                        This thank you note has been sent!
                      </div>
                    </div>
                  </div>
                ) : (
                  <div className="flex justify-end space-x-3 pt-4">
                    <button
                      type="button"
                      onClick={() => {
                        setSelectedGift(null);
                        setExistingNote(null);
                        setFormData({ message: '', imageUrl: '' });
                      }}
                      className="px-4 py-2 border border-gray-300 shadow-sm text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50"
                    >
                      Cancel
                    </button>
                    <button
                      type="submit"
                      disabled={isSubmitting || !formData.message.trim()}
                      className="px-4 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-primary-600 hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500 disabled:opacity-50"
                    >
                      {isSubmitting ? 'Saving...' : existingNote ? 'Update Draft' : 'Save Draft'}
                    </button>
                    {existingNote && !existingNote.isSent && (
                      <button
                        type="button"
                        onClick={handleSend}
                        disabled={isSubmitting}
                        className="inline-flex items-center px-4 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-green-600 hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-green-500 disabled:opacity-50"
                      >
                        <Send className="w-4 h-4 mr-2" />
                        {isSubmitting ? 'Sending...' : 'Send Now'}
                      </button>
                    )}
                  </div>
                )}
              </form>
            </div>
          ) : (
            <div className="bg-gray-50 rounded-lg border border-gray-200 p-12 text-center">
              <Heart className="mx-auto h-12 w-12 text-gray-300" />
              <p className="mt-2 text-sm text-gray-500">
                Select a gift from the list to write a thank you note
              </p>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};
