"use client";

import styled from "styled-components";
import type { ColorStyleProps } from "@/theme/style-props";
import type { AppUiTheme } from "@/theme/ui-theme";

const asAppTheme = (theme: unknown) => theme as AppUiTheme;

export const StyledAppHeader = styled.header<ColorStyleProps>`
  position: sticky;
  top: 0;
  z-index: 10;
  backdrop-filter: blur(14px);
  background: ${({ theme, $backgroundColor = "background" }) =>
    `linear-gradient(180deg, ${asAppTheme(theme).colors[$backgroundColor]}, rgba(246, 243, 238, 0.72))`};
  border-bottom: 1px solid
    ${({ theme, $borderColor = "border" }) => asAppTheme(theme).colors[$borderColor]};
`;

export const StyledAppHeaderInner = styled.div`
  width: ${({ theme }) => asAppTheme(theme).layout.contentWidth};
  margin: 0 auto;
  padding: 18px 0;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;

  ${({ theme }) => asAppTheme(theme).breakpoints.down("sm")} {
    width: ${({ theme }) => asAppTheme(theme).layout.mobileContentWidth};
  }
`;

export const StyledAppLogo = styled.a<ColorStyleProps>`
  display: inline-flex;
  align-items: center;
  gap: 12px;
  color: ${({ theme, $textColor = "darkBlue" }) => asAppTheme(theme).colors[$textColor]};
  text-decoration: none;
`;

export const StyledAppLogoMark = styled.span`
  width: 14px;
  height: 14px;
  border-radius: ${({ theme }) => asAppTheme(theme).radius.pill};
  background: ${({ theme }) =>
    `linear-gradient(135deg, ${asAppTheme(theme).colors.formulaRed}, ${asAppTheme(theme).colors.formulaRedDark})`};
  box-shadow: 0 0 0 6px rgba(225, 6, 0, 0.12);
`;

export const StyledAppLogoText = styled.span`
  font-size: 1.1rem;
  font-weight: 800;
  letter-spacing: 0.06em;
  text-transform: uppercase;
`;

export const StyledHeaderActions = styled.div`
  display: flex;
  align-items: center;
  margin-left: auto;
`;
