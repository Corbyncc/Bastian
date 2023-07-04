import React from 'react';
import { render } from 'react-dom';
import { MemoryRouter } from 'react-router-dom';
import App from './App';

it('renders without crashing', async () => {
    const div = document.createElement('div');
    render(
        <MemoryRouter>
            <App />
        </MemoryRouter>,
        div
    );
    await new Promise((resolve) => setTimeout(resolve, 1000));
});
