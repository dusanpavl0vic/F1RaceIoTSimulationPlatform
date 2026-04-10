"use client";

import styled from "styled-components";
import type { ColorStyleProps } from "@/theme/style-props";
import type { AppUiTheme } from "@/theme/ui-theme";

const asAppTheme = (theme: unknown) => theme as AppUiTheme;

export const StyledAppFooter = styled.footer<ColorStyleProps>`
  margin-top: auto;
  border-top: 1px solid
    ${({ theme, $borderColor = "border" }) => asAppTheme(theme).colors[$borderColor]};
  background: linear-gradient(180deg, rgba(17, 17, 17, 0.04), rgba(17, 17, 17, 0.02));
`;

export const StyledAppFooterInner = styled.div`
  width: ${({ theme }) => asAppTheme(theme).layout.contentWidth};
  margin: 0 auto;
  padding: 20px 0 30px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 18px;

  ${({ theme }) => asAppTheme(theme).breakpoints.down("sm")} {
    width: ${({ theme }) => asAppTheme(theme).layout.mobileContentWidth};
    flex-direction: column;
    align-items: flex-start;
  }
`;

export const StyledAppFooterLink = styled.a<ColorStyleProps>`
  display: inline-flex;
  align-items: center;
  min-height: 42px;
  padding: 0 16px;
  border-radius: ${({ theme }) => asAppTheme(theme).radius.pill};
  background: ${({ theme, $backgroundColor = "formulaRed" }) =>
    `linear-gradient(135deg, ${asAppTheme(theme).colors[$backgroundColor]}, ${asAppTheme(theme).colors.formulaRedDark})`};
  color: ${({ theme, $textColor = "white" }) => asAppTheme(theme).colors[$textColor]};
  text-decoration: none;
  font-weight: 700;
  box-shadow: ${({ theme }) => asAppTheme(theme).colors.accentShadow};
`;
