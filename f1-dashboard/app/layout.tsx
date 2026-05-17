import "./globals.css";
import { AppRouterCacheProvider } from "@mui/material-nextjs/v15-appRouter";
import { Silkscreen } from "next/font/google";
// @ts-ignore
import { AppShell } from "@/components/AppShell/app-shell";
import { AppProvider } from "@/components/providers/app-provider";
import type { Metadata } from "next";

const silkscreen = Silkscreen({
  weight: ["400", "700"],
  subsets: ["latin"],
  display: "swap",
  variable: "--font-silkscreen",
});

export const metadata: Metadata = {
  title: "F1 Dashboard",
  description: "Next.js frontend projekat",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en" className={silkscreen.variable} suppressHydrationWarning>
      <body className={silkscreen.className} suppressHydrationWarning>
        <AppRouterCacheProvider options={{ enableCssLayer: true }}>
          <AppProvider>
            <AppShell>{children}</AppShell>
          </AppProvider>
        </AppRouterCacheProvider>
      </body>
    </html>
  );
}
