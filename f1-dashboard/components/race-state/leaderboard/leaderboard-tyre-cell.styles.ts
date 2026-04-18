"use client";

import { Box, Typography } from "@mui/material";
import styled from "styled-components";

export const StyledTyreWrapper = styled(Box)`
  display: flex;
  align-items: center;
  gap: 6px;
`;

export const StyledTyreLapCount = styled(Typography)`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 0.60rem;
  color: ${({ theme }) => theme.colors.textMuted};
`;
