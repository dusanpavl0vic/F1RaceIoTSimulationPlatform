import { appColors, type AppColorToken } from "@/theme/colors";

export const breakpointValues = {
  xs: 0,
  sm: 640,
  md: 960,
  lg: 1280,
  xl: 1536,
} as const;

export type AppBreakpointKey = keyof typeof breakpointValues;

const toMaxWidth = (value: number) => Math.max(value - 0.02, 0);

export const appBreakpoints = {
  values: breakpointValues,
  up: (key: AppBreakpointKey) =>
    `@media (min-width: ${breakpointValues[key]}px)`,
  down: (key: AppBreakpointKey) =>
    `@media (max-width: ${toMaxWidth(breakpointValues[key])}px)`,
  between: (start: AppBreakpointKey, end: AppBreakpointKey) =>
    `@media (min-width: ${breakpointValues[start]}px) and (max-width: ${toMaxWidth(
      breakpointValues[end]
    )}px)`,
};

export const appDevices = {
  mobile: appBreakpoints.down("sm"),
  tablet: appBreakpoints.between("sm", "lg"),
  desktop: appBreakpoints.up("lg"),
} as const;

export type AppUiColors = Record<AppColorToken, string>;

export type AppUiTheme = {
  colors: AppUiColors;
  breakpoints: typeof appBreakpoints;
  devices: typeof appDevices;
  layout: {
    contentWidth: string;
    mobileContentWidth: string;
  };
  radius: {
    panel: string;
    card: string;
    pill: string;
  };
};

export const appUiTheme: AppUiTheme = {
  colors: appColors,
  breakpoints: appBreakpoints,
  devices: appDevices,
  layout: {
    contentWidth: "min(1240px, calc(100vw - 32px))",
    mobileContentWidth: "min(100vw - 20px, 100%)",
  },
  radius: {
    panel: "28px",
    card: "22px",
    pill: "999px",
  },
};

export const resolveThemeColor = (token: AppColorToken) => appUiTheme.colors[token];
