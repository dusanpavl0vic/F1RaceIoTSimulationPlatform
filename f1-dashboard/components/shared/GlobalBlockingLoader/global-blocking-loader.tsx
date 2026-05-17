"use client";

import {
  StyledGlobalBlockingLoaderBackdrop,
  StyledGlobalBlockingLoaderCard,
  StyledGlobalBlockingLoaderLabel,
  StyledGlobalBlockingLoaderSpinner,
} from "./global-blocking-loader.styles";

type GlobalBlockingLoaderProps = {
  open: boolean;
  label: string;
};

export function GlobalBlockingLoader({
  open,
  label,
}: GlobalBlockingLoaderProps) {
  return (
    <StyledGlobalBlockingLoaderBackdrop open={open}>
      <StyledGlobalBlockingLoaderCard>
        <StyledGlobalBlockingLoaderSpinner size={38} thickness={4.6} />
        <StyledGlobalBlockingLoaderLabel>{label}</StyledGlobalBlockingLoaderLabel>
      </StyledGlobalBlockingLoaderCard>
    </StyledGlobalBlockingLoaderBackdrop>
  );
}
