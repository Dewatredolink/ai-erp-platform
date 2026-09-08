import { Route, BrowserRouter, Routes } from 'react-router-dom';
import Navigation from './components/Navigation';
import CompaniesPage from './pages/CompaniesPage';
import './App.css';

function App() {
  return (
    <BrowserRouter>
      <Navigation />
      <main>
        <Routes>
          <Route path="/" element={<CompaniesPage />} />
        </Routes>
      </main>
    </BrowserRouter>
  );
}

export default App;
