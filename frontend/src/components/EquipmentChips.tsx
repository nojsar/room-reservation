import CoPresentIcon from '@mui/icons-material/CoPresent';
import DrawIcon from '@mui/icons-material/Draw';
import TvIcon from '@mui/icons-material/Tv';
import VideoCallIcon from '@mui/icons-material/VideoCall';
import { Chip, Stack, Typography } from '@mui/material';
import type { ReactElement } from 'react';
import type { Equipment } from '../api/types';

export const EQUIPMENT_LABELS: Record<Equipment, string> = {
  Projector: 'Projector',
  TvScreen: 'TV screen',
  VideoConferencing: 'Video conferencing',
  Whiteboard: 'Whiteboard',
};

const EQUIPMENT_ICONS: Record<Equipment, ReactElement> = {
  Projector: <CoPresentIcon fontSize="small" />,
  TvScreen: <TvIcon fontSize="small" />,
  VideoConferencing: <VideoCallIcon fontSize="small" />,
  Whiteboard: <DrawIcon fontSize="small" />,
};

export function EquipmentChips({ equipment }: { equipment: Equipment[] }) {
  if (equipment.length === 0) {
    return (
      <Typography variant="body2" color="text.secondary">
        None
      </Typography>
    );
  }

  return (
    <Stack direction="row" spacing={0.5} flexWrap="wrap" useFlexGap>
      {equipment.map((item) => (
        <Chip key={item} icon={EQUIPMENT_ICONS[item]} label={EQUIPMENT_LABELS[item]} size="small" variant="outlined" />
      ))}
    </Stack>
  );
}
