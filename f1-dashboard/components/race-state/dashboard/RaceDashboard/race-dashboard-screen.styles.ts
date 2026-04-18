"use client";

import { Box } from "@mui/material";
import styled from "styled-components";

export const StyledDashboardShell = styled(Box)`
  width: 100%;
  padding: 20px 0 56px;
  display: flex;
  flex-direction: column;
  gap: 12px;
`;

export const StyledMainGrid = styled(Box)`
  display: grid;
  grid-template-columns: 1fr 300px;
  gap: 12px;
  align-items: start;

  @media (max-width: 1535px) {
    grid-template-columns: 1fr;
  }
`;

export const StyledSideStack = styled(Box)`
  display: flex;
  flex-direction: column;
  gap: 12px;
`;
