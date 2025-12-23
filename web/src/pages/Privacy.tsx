import React from 'react';
import { Link } from 'react-router-dom';

export const Privacy: React.FC = () => {
  return (
    <div className="min-h-screen bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-3xl mx-auto">
        <div className="mb-8">
          <Link
            to="/login"
            className="text-primary-600 hover:text-primary-500 text-sm font-medium"
          >
            &larr; Back to Login
          </Link>
        </div>

        <div className="bg-white shadow rounded-lg p-8">
          <h1 className="text-3xl font-bold text-gray-900 mb-2">Privacy Policy</h1>
          <p className="text-sm text-gray-500 mb-8">Last updated: December 23, 2024</p>

          <div className="prose prose-gray max-w-none space-y-6">
            <section>
              <h2 className="text-xl font-semibold text-gray-900 mb-3">Overview</h2>
              <p className="text-gray-600">
                Earn &amp; Learn ("we", "our", or "us") is committed to protecting your privacy.
                This Privacy Policy explains how we collect, use, and safeguard your information
                when you use our mobile application and web service.
              </p>
            </section>

            <section>
              <h2 className="text-xl font-semibold text-gray-900 mb-3">Information We Collect</h2>
              <p className="text-gray-600 mb-3">
                We collect information that you provide directly to us:
              </p>
              <ul className="list-disc pl-6 text-gray-600 space-y-2">
                <li>
                  <strong>Account Information:</strong> Email address, name, and password when you
                  create an account.
                </li>
                <li>
                  <strong>Family Information:</strong> Names of family members and children you add
                  to your family group.
                </li>
                <li>
                  <strong>Financial Information:</strong> Allowance amounts, transaction records,
                  and balance information that you enter into the app.
                </li>
                <li>
                  <strong>Wish List Data:</strong> Items, prices, and notes you add to wish lists.
                </li>
              </ul>
            </section>

            <section>
              <h2 className="text-xl font-semibold text-gray-900 mb-3">How We Use Your Information</h2>
              <p className="text-gray-600 mb-3">We use the information we collect to:</p>
              <ul className="list-disc pl-6 text-gray-600 space-y-2">
                <li>Provide, maintain, and improve our services</li>
                <li>Process transactions and track allowance balances</li>
                <li>Send you technical notices and support messages</li>
                <li>Respond to your comments and questions</li>
                <li>Protect against fraudulent or unauthorized activity</li>
              </ul>
            </section>

            <section>
              <h2 className="text-xl font-semibold text-gray-900 mb-3">Information Sharing</h2>
              <p className="text-gray-600">
                We do not sell, trade, or rent your personal information to third parties.
                We may share your information only in the following circumstances:
              </p>
              <ul className="list-disc pl-6 text-gray-600 space-y-2 mt-3">
                <li>With your consent or at your direction</li>
                <li>To comply with legal obligations</li>
                <li>To protect our rights, privacy, safety, or property</li>
                <li>
                  With service providers who assist in our operations (e.g., hosting providers),
                  under strict confidentiality agreements
                </li>
              </ul>
            </section>

            <section>
              <h2 className="text-xl font-semibold text-gray-900 mb-3">Data Security</h2>
              <p className="text-gray-600">
                We implement appropriate technical and organizational measures to protect your
                personal information, including:
              </p>
              <ul className="list-disc pl-6 text-gray-600 space-y-2 mt-3">
                <li>Encryption of data in transit using TLS/SSL</li>
                <li>Secure password hashing</li>
                <li>Regular security assessments</li>
                <li>Access controls limiting who can view your data</li>
              </ul>
            </section>

            <section>
              <h2 className="text-xl font-semibold text-gray-900 mb-3">Data Retention</h2>
              <p className="text-gray-600">
                We retain your information for as long as your account is active or as needed to
                provide you services. You may request deletion of your account and associated data
                at any time by contacting us.
              </p>
            </section>

            <section>
              <h2 className="text-xl font-semibold text-gray-900 mb-3">Children's Privacy</h2>
              <p className="text-gray-600">
                Our service is designed for family use, including children. Child accounts are
                created and managed by parents/guardians. We collect only the minimum information
                necessary to provide the service (name and transaction data). Parents can review,
                modify, or delete their children's information at any time.
              </p>
            </section>

            <section>
              <h2 className="text-xl font-semibold text-gray-900 mb-3">Your Rights</h2>
              <p className="text-gray-600 mb-3">You have the right to:</p>
              <ul className="list-disc pl-6 text-gray-600 space-y-2">
                <li>Access the personal information we hold about you</li>
                <li>Request correction of inaccurate information</li>
                <li>Request deletion of your account and data</li>
                <li>Export your data in a portable format</li>
                <li>Withdraw consent where applicable</li>
              </ul>
            </section>

            <section>
              <h2 className="text-xl font-semibold text-gray-900 mb-3">Third-Party Services</h2>
              <p className="text-gray-600">
                We use the following third-party services to operate our app:
              </p>
              <ul className="list-disc pl-6 text-gray-600 space-y-2 mt-3">
                <li>
                  <strong>Microsoft Azure:</strong> Cloud hosting and database services
                  (United States)
                </li>
                <li>
                  <strong>Azure Communication Services:</strong> Email delivery for password
                  resets and notifications
                </li>
              </ul>
            </section>

            <section>
              <h2 className="text-xl font-semibold text-gray-900 mb-3">Changes to This Policy</h2>
              <p className="text-gray-600">
                We may update this Privacy Policy from time to time. We will notify you of any
                changes by posting the new Privacy Policy on this page and updating the "Last
                updated" date.
              </p>
            </section>

            <section>
              <h2 className="text-xl font-semibold text-gray-900 mb-3">Contact Us</h2>
              <p className="text-gray-600">
                If you have any questions about this Privacy Policy or our practices, please
                contact us at:
              </p>
              <p className="text-gray-600 mt-3">
                <strong>Email:</strong>{' '}
                <a
                  href="mailto:privacy@earnandlearn.app"
                  className="text-primary-600 hover:text-primary-500"
                >
                  privacy@earnandlearn.app
                </a>
              </p>
            </section>
          </div>
        </div>

        <div className="mt-8 text-center text-sm text-gray-500">
          <p>&copy; {new Date().getFullYear()} Earn &amp; Learn. All rights reserved.</p>
        </div>
      </div>
    </div>
  );
};
