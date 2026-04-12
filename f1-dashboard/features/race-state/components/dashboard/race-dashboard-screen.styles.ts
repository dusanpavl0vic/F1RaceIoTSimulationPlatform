"use client";

import styled from "styled-components";

export const StyledDashboardShell = styled.div`
  width: 100%;
  padding: 20px 0 56px;
  display: flex;
  flex-direction: column;
  gap: 12px;
`;

export const StyledMainGrid = styled.div`
  display: grid;
  grid-template-columns: 1fr 300px;
  gap: 12px;
  align-items: start;

  @media (max-width: 1535px) {
    grid-template-columns: 1fr;
  }
`;

export const StyledSideStack = styled.div`
  display: flex;
  flex-direction: column;
  gap: 12px;
`;
