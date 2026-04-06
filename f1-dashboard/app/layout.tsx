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
        <AppProvider>
          <AppShell>{children}</AppShell>
        </AppProvider>
      </body>
    </html>
  );
}
