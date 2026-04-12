"use client";

import styled from "styled-components";
import { t } from "@/theme/styled-helpers";

export const StyledTyreWrapper = styled.div`
  display: flex;
  align-items: center;
  gap: 6px;
`;

export const StyledTyreLapCount = styled.span`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 0.60rem;
  color: ${({ theme }) => t(theme).colors.textMuted};
`;
