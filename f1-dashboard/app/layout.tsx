import { AppRouterCacheProvider } from "@mui/material-nextjs/v15-appRouter";
import { AppShell } from "@/components/layout/app-shell";
import { AppProvider } from "@/components/providers/app-provider";
import type { Metadata } from "next";

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
    <html lang="en">
      <body>
        <AppRouterCacheProvider options={{ enableCssLayer: true }}>
          <AppProvider>
            <AppShell>{children}</AppShell>
          </AppProvider>
        </AppRouterCacheProvider>
      </body>
    </html>
  );
}
