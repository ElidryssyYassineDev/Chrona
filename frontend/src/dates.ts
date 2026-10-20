// Date locale -> 'AAAA-MM-JJ' (pas de toISOString : il passe par l'UTC et peut décaler le jour).
export function toDateString(date: Date): string {
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${date.getFullYear()}-${month}-${day}`
}

// Semaine courante, du lundi au dimanche : la période proposée par l'interface (l'API accepte toute période).
export function currentWeek(today: Date = new Date()): { start: string; end: string } {
  const offset = (today.getDay() + 6) % 7 // lundi = 0
  const monday = new Date(today.getFullYear(), today.getMonth(), today.getDate() - offset)
  const sunday = new Date(monday.getFullYear(), monday.getMonth(), monday.getDate() + 6)
  return { start: toDateString(monday), end: toDateString(sunday) }
}