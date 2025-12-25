import React from 'react';
import { Link } from 'react-router-dom';

export const Terms: React.FC = () => {
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
          <h1 className="text-3xl font-bold text-gray-900 mb-2">Terms of Service</h1>
          <p className="text-sm text-gray-500 mb-8">Last updated: December 25, 2024</p>

          <div className="prose prose-gray max-w-none space-y-6">
            <section>
              <h2 className="text-xl font-semibold text-gray-900 mb-3">Agreement to Terms</h2>
              <p className="text-gray-600">
                By accessing or using Earn &amp; Learn ("the Service"), you agree to be bound by these
                Terms of Service. If you disagree with any part of these terms, you may not access
                the Service.
              </p>
            </section>

            <section>
              <h2 className="text-xl font-semibold text-gray-900 mb-3">Description of Service</h2>
              <p className="text-gray-600">
                Earn &amp; Learn is a family allowance tracking application that helps parents manage
                children's allowances, track spending, and teach financial responsibility. The Service
                includes web and mobile applications for tracking balances, transactions, wish lists,
                and savings goals.
              </p>
            </section>

            <section>
              <h2 className="text-xl font-semibold text-gray-900 mb-3">User Accounts</h2>
              <p className="text-gray-600 mb-3">
                To use the Service, you must create an account. You agree to:
              </p>
              <ul className="list-disc pl-6 text-gray-600 space-y-2">
                <li>Provide accurate and complete information when creating your account</li>
                <li>Maintain the security of your account credentials</li>
                <li>Promptly notify us of any unauthorized access to your account</li>
                <li>Accept responsibility for all activities that occur under your account</li>
              </ul>
              <p className="text-gray-600 mt-3">
                Parent accounts are responsible for creating and managing child accounts within their
                family. Parents must have legal authority to create accounts for minors in their care.
              </p>
            </section>

            <section>
              <h2 className="text-xl font-semibold text-gray-900 mb-3">Acceptable Use</h2>
              <p className="text-gray-600 mb-3">You agree not to:</p>
              <ul className="list-disc pl-6 text-gray-600 space-y-2">
                <li>Use the Service for any illegal purpose or in violation of any laws</li>
                <li>Attempt to gain unauthorized access to any part of the Service</li>
                <li>Interfere with or disrupt the Service or servers</li>
                <li>Upload malicious code or attempt to compromise security</li>
                <li>Impersonate another person or entity</li>
                <li>Use the Service to track real financial accounts or actual bank balances</li>
              </ul>
            </section>

            <section>
              <h2 className="text-xl font-semibold text-gray-900 mb-3">Service Limitations</h2>
              <p className="text-gray-600">
                Earn &amp; Learn is an educational and organizational tool only. It is not a financial
                institution and does not hold, transfer, or manage actual money. All balances and
                transactions within the app are for tracking purposes only and represent allowances
                managed outside of the application.
              </p>
            </section>

            <section>
              <h2 className="text-xl font-semibold text-gray-900 mb-3">Intellectual Property</h2>
              <p className="text-gray-600">
                The Service and its original content, features, and functionality are owned by
                Earn &amp; Learn and are protected by international copyright, trademark, and other
                intellectual property laws. You may not copy, modify, distribute, or create derivative
                works based on the Service without our express written permission.
              </p>
            </section>

            <section>
              <h2 className="text-xl font-semibold text-gray-900 mb-3">User Content</h2>
              <p className="text-gray-600">
                You retain ownership of any content you submit to the Service (such as transaction
                descriptions and wish list items). By submitting content, you grant us a license to
                use, store, and display that content solely for the purpose of providing the Service
                to you.
              </p>
            </section>

            <section>
              <h2 className="text-xl font-semibold text-gray-900 mb-3">Termination</h2>
              <p className="text-gray-600">
                We may terminate or suspend your account at any time, without prior notice, for
                conduct that we believe violates these Terms or is harmful to other users, us, or
                third parties. You may also delete your account at any time through the app settings
                or by contacting us.
              </p>
            </section>

            <section>
              <h2 className="text-xl font-semibold text-gray-900 mb-3">Disclaimer of Warranties</h2>
              <p className="text-gray-600">
                The Service is provided "as is" and "as available" without warranties of any kind,
                either express or implied. We do not warrant that the Service will be uninterrupted,
                error-free, or secure. We are not responsible for any loss of data or any damages
                resulting from your use of the Service.
              </p>
            </section>

            <section>
              <h2 className="text-xl font-semibold text-gray-900 mb-3">Limitation of Liability</h2>
              <p className="text-gray-600">
                To the maximum extent permitted by law, Earn &amp; Learn shall not be liable for any
                indirect, incidental, special, consequential, or punitive damages, or any loss of
                profits or revenues, whether incurred directly or indirectly, or any loss of data,
                use, goodwill, or other intangible losses resulting from your use of the Service.
              </p>
            </section>

            <section>
              <h2 className="text-xl font-semibold text-gray-900 mb-3">Changes to Terms</h2>
              <p className="text-gray-600">
                We reserve the right to modify these Terms at any time. We will notify users of any
                material changes by posting the new Terms on this page and updating the "Last updated"
                date. Your continued use of the Service after such changes constitutes acceptance of
                the new Terms.
              </p>
            </section>

            <section>
              <h2 className="text-xl font-semibold text-gray-900 mb-3">Governing Law</h2>
              <p className="text-gray-600">
                These Terms shall be governed by and construed in accordance with the laws of the
                United States, without regard to its conflict of law provisions.
              </p>
            </section>

            <section>
              <h2 className="text-xl font-semibold text-gray-900 mb-3">Contact Us</h2>
              <p className="text-gray-600">
                If you have any questions about these Terms of Service, please contact us at:
              </p>
              <p className="text-gray-600 mt-3">
                <strong>Email:</strong>{' '}
                <a
                  href="mailto:support@earnandlearn.app"
                  className="text-primary-600 hover:text-primary-500"
                >
                  support@earnandlearn.app
                </a>
              </p>
            </section>
          </div>
        </div>

        <div className="mt-8 text-center text-sm text-gray-500 space-y-2">
          <p>
            <Link to="/privacy" className="text-primary-600 hover:text-primary-500">
              Privacy Policy
            </Link>
          </p>
          <p>&copy; {new Date().getFullYear()} Earn &amp; Learn. All rights reserved.</p>
        </div>
      </div>
    </div>
  );
};
