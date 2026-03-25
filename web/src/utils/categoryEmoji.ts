import type { TransactionCategory } from '../types';

interface CategoryInfo {
  emoji: string;
  color: string; // tailwind bg class
}

const categoryMap: Record<string, CategoryInfo> = {
  Allowance:      { emoji: '💰', color: 'bg-primary-100' },
  Chores:         { emoji: '🧹', color: 'bg-secondary-50' },
  Gift:           { emoji: '🎁', color: 'bg-pink-100' },
  BonusReward:    { emoji: '⭐', color: 'bg-secondary-100' },
  Task:           { emoji: '📋', color: 'bg-secondary-50' },
  OtherIncome:    { emoji: '💵', color: 'bg-primary-50' },
  Toys:           { emoji: '🧸', color: 'bg-orange-100' },
  Games:          { emoji: '🎮', color: 'bg-purple-100' },
  Books:          { emoji: '📚', color: 'bg-tertiary-100' },
  Clothes:        { emoji: '👕', color: 'bg-cyan-100' },
  Snacks:         { emoji: '🍕', color: 'bg-red-100' },
  Candy:          { emoji: '🍬', color: 'bg-pink-100' },
  Electronics:    { emoji: '📱', color: 'bg-slate-100' },
  Entertainment:  { emoji: '🎬', color: 'bg-violet-100' },
  Sports:         { emoji: '⚽', color: 'bg-green-100' },
  Crafts:         { emoji: '🎨', color: 'bg-amber-100' },
  OtherSpending:  { emoji: '🛒', color: 'bg-gray-100' },
  Savings:        { emoji: '🏦', color: 'bg-tertiary-50' },
  Charity:        { emoji: '💝', color: 'bg-rose-100' },
  Investment:     { emoji: '📈', color: 'bg-emerald-100' },
};

const defaultCategory: CategoryInfo = { emoji: '💵', color: 'bg-gray-100' };

export function getCategoryInfo(category: TransactionCategory | string): CategoryInfo {
  return categoryMap[category] || defaultCategory;
}
