"use client";

import {
  dismissBattleAlert,
  selectBattleAlerts,
} from "@/features/store/race-state/raceStateUiSlice";
import { Typography } from "@mui/material";
import { useDispatch, useSelector } from "react-redux";
import styled from "styled-components";

const MAX_VISIBLE_ALERTS = 5;

function formatAlertTime(value: string) {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return value;
  }

  return new Intl.DateTimeFormat("sr-RS", {
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
    hour12: false,
  }).format(date);
}

const PanelShell = styled.aside`
  position: sticky;
  top: 16px;
  display: flex;
  flex-direction: column;
  gap: 12px;
  min-width: 0;
`;

const PanelCard = styled.section`
  background: ${({ theme }) => theme.colors.panel};
  box-shadow: ${({ theme }) => theme.colors.shadow};
  border-radius: 16px;
  overflow: hidden;
`;

const PanelHeader = styled.div`
  padding: 14px 16px 12px;
  border-bottom: 1px solid ${({ theme }) => theme.colors.border};
  background: ${({ theme }) => theme.colors.backgroundSoft};
`;

const PanelEyebrow = styled(Typography)`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 11px;
  line-height: 1.1;
  letter-spacing: 0.16em;
  color: ${({ theme }) => theme.colors.formulaRed};
`;

const PanelTitle = styled(Typography)`
  margin-top: 6px;
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 14px;
  line-height: 1.35;
  color: ${({ theme }) => theme.colors.textPrimary};
`;

const PanelSubtitle = styled(Typography)`
  margin-top: 6px;
  font-size: 11px;
  line-height: 1.5;
  color: ${({ theme }) => theme.colors.textMuted};
`;

const AlertList = styled.div`
  display: flex;
  flex-direction: column;
`;

const AlertItem = styled.article<{ $highlighted: boolean }>`
  padding: 14px 16px;
  border-bottom: 1px solid ${({ theme }) => theme.colors.border};
  background: ${({ $highlighted, theme }) =>
    $highlighted ? theme.colors.backgroundSoft : theme.colors.panel};

  &:last-child {
    border-bottom: 0;
  }
`;

const AlertMetaRow = styled.div`
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
`;

const AlertBadge = styled.span<{ $highlighted: boolean }>`
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 44px;
  padding: 3px 8px;
  border-radius: 999px;
  border: 1px solid
    ${({ $highlighted, theme }) =>
      $highlighted ? "rgba(220, 0, 0, 0.28)" : theme.colors.border};
  background: ${({ $highlighted, theme }) =>
    $highlighted ? "rgba(220, 0, 0, 0.10)" : theme.colors.backgroundSoft};
  color: ${({ $highlighted, theme }) =>
    $highlighted ? theme.colors.formulaRed : theme.colors.textMuted};
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 9px;
  line-height: 1;
`;

const AlertMetaText = styled(Typography)`
  font-size: 10px;
  line-height: 1.3;
  color: ${({ theme }) => theme.colors.textMuted};
`;

const AlertMessage = styled(Typography)<{ $highlighted: boolean }>`
  margin-top: 10px;
  font-size: 12px;
  line-height: 1.6;
  color: ${({ theme }) => theme.colors.textPrimary};
  font-weight: ${({ $highlighted }) => ($highlighted ? 700 : 500)};
`;

const AlertDrivers = styled(Typography)`
  margin-top: 8px;
  font-size: 10px;
  line-height: 1.4;
  color: ${({ theme }) => theme.colors.textMuted};
`;

const CloseButton = styled.button`
  margin-left: auto;
  border: 0;
  background: transparent;
  color: ${({ theme }) => theme.colors.textMuted};
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 13px;
  line-height: 1;
  cursor: pointer;
  padding: 0;

  &:hover {
    color: ${({ theme }) => theme.colors.formulaRed};
  }
`;

const EmptyState = styled.div`
  padding: 18px 16px;
`;

const EmptyTitle = styled(Typography)`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 11px;
  line-height: 1.4;
  color: ${({ theme }) => theme.colors.textPrimary};
`;

const EmptyText = styled(Typography)`
  margin-top: 8px;
  font-size: 11px;
  line-height: 1.5;
  color: ${({ theme }) => theme.colors.textMuted};
`;

export function BattleAlertsOverlay() {
  const dispatch = useDispatch();
  const alerts = useSelector(selectBattleAlerts);
  const visibleAlerts = alerts.slice(-MAX_VISIBLE_ALERTS).reverse();

  return (
    <PanelShell>
      <PanelCard>
        <PanelHeader>
          <PanelEyebrow>LIVE ALERTS</PanelEyebrow>
          <PanelTitle>Battle Feed</PanelTitle>
          <PanelSubtitle>
            Drivers within one second of the car ahead from live timing.
          </PanelSubtitle>
        </PanelHeader>

        {visibleAlerts.length === 0 ? (
          <EmptyState>
            <EmptyTitle>No active battle alerts</EmptyTitle>
            <EmptyText>
              When the canonical timing feed detects a gap under one second, the
              event will appear here.
            </EmptyText>
          </EmptyState>
        ) : (
          <AlertList>
            {visibleAlerts.map((alert, index) => {
              const isHighlighted = index === 0;

              return (
                <AlertItem key={alert.id} $highlighted={isHighlighted}>
                  <AlertMetaRow>
                    <AlertBadge $highlighted={isHighlighted}>
                      P{alert.battleForPosition}
                    </AlertBadge>
                    <AlertMetaText>{alert.gapLabel} GAP</AlertMetaText>
                    <AlertMetaText>
                      {formatAlertTime(alert.sentAt)}
                    </AlertMetaText>
                    <CloseButton
                      type="button"
                      onClick={() => {
                        dispatch(dismissBattleAlert(alert.id));
                      }}
                    >
                      X
                    </CloseButton>
                  </AlertMetaRow>

                  <AlertMessage $highlighted={isHighlighted}>
                    {alert.message}
                  </AlertMessage>
                  <AlertDrivers>
                    Driver #{alert.driverNumber} vs #{alert.aheadDriverNumber}
                  </AlertDrivers>
                </AlertItem>
              );
            })}
          </AlertList>
        )}
      </PanelCard>
    </PanelShell>
  );
}
