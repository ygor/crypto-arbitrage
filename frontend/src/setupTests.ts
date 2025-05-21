// jest-dom adds custom jest matchers for asserting on DOM nodes.
// allows you to do things like:
// expect(element).toHaveTextContent(/react/i)
// learn more: https://github.com/testing-library/jest-dom
import '@testing-library/jest-dom';
import React from 'react';

// Fix for act() deprecation warning
// This ensures all tests use React.act instead of ReactDOMTestUtils.act
jest.mock('react-dom/test-utils', () => {
  const mockReact = require('react');
  const originalModule = jest.requireActual('react-dom/test-utils');
  return {
    ...originalModule,
    act: mockReact.act,
  };
});

// Mock window.ENV for tests
window.ENV = {
  apiUrl: 'http://localhost:5001/api',
  signalrUrl: 'http://localhost:5001/hubs'
};
