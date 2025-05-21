// jest-dom adds custom jest matchers for asserting on DOM nodes.
// allows you to do things like:
// expect(element).toHaveTextContent(/react/i)
// learn more: https://github.com/testing-library/jest-dom
import '@testing-library/jest-dom';

// Mock ResizeObserver which isn't available in Jest test environment
class ResizeObserverMock {
  observe() {}
  unobserve() {}
  disconnect() {}
}
window.ResizeObserver = ResizeObserverMock;

// Suppress expected console errors during tests
const originalConsoleError = console.error;
console.error = (...args) => {
  // Skip logging "Error fetching statistics" errors as they're expected in tests
  if (args[0] === 'Error fetching statistics:') {
    return;
  }
  
  originalConsoleError(...args);
}; 