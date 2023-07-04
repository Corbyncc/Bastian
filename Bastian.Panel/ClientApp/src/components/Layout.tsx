import React from 'react';
import { Container } from 'reactstrap';
import { NavMenu } from './NavMenu/NavMenu';

export function Layout(props: { children: React.ReactNode }) {
    return (
        <div>
            <NavMenu />
            <Container tag="main">
                {props.children}
            </Container>
        </div>
    );
}
