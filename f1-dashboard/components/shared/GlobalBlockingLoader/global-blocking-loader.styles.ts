"use client";

import { appColors } from "@/theme/colors";
import { Backdrop, Box, CircularProgress, Typography } from "@mui/material";
import styled from "styled-components";

export const StyledGlobalBlockingLoaderBackdrop = styled(Backdrop)`
  && {
    z-index: 1600;
    background: rgba(5, 14, 60, 0.56);
    backdrop-filter: blur(6px);
    color: ${appColors.white};
  }
`;

export const StyledGlobalBlockingLoaderCard = styled(Box)`
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 14px;
  min-width: 240px;
  padding: 24px 28px;
  border-radius: 18px;
  border: 1px solid rgba(255, 255, 255, 0.1);
  background: linear-gradient(180deg, rgba(11, 22, 74, 0.96) 0%, rgba(5, 14, 60, 0.98) 100%);
  box-shadow: 0 20px 80px rgba(0, 0, 0, 0.35);
`;

export const StyledGlobalBlockingLoaderSpinner = styled(CircularProgress)`
  && {
    color: ${appColors.formulaRed};
  }
`;

export const StyledGlobalBlockingLoaderLabel = styled(Typography)`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 11px;
  color: ${appColors.white};
  text-align: center;
  line-height: 1.4;
`;
